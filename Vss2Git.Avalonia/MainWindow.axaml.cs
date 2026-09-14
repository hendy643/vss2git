using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace Hpdi.Vss2Git.Avalonia
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<int, EncodingInfo> codePages = new();
        private readonly WorkQueue workQueue = new(1);
        private readonly DispatcherTimer statusTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
        private readonly Logger logger = Logger.Null;

        private MigrationOrchestrator? orchestrator;

        public MainWindow()
        {
            InitializeComponent();

            statusTimer.Tick += StatusTimer_Tick;
            Opened += MainWindow_Opened;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Opened(object? sender, EventArgs e)
        {
            Title += " " + Assembly.GetExecutingAssembly().GetName().Version;

            var systemEncoding = GetSystemDefaultEncoding();
            var defaultCodePage = systemEncoding.CodePage;
            var encodingItems = new List<string> { $"System default - {systemEncoding.EncodingName}" };
            var defaultIndex = 0;

            foreach (var encoding in Encoding.GetEncodings())
            {
                var index = encodingItems.Count;
                encodingItems.Add($"CP{encoding.CodePage} - {encoding.DisplayName}");
                codePages[index] = encoding;
                if (encoding.CodePage == defaultCodePage)
                {
                    codePages[defaultIndex] = encoding;
                }
            }
            EncodingComboBox.ItemsSource = encodingItems;
            EncodingComboBox.SelectedIndex = defaultIndex;

            GitBackendComboBox.ItemsSource = Enum.GetNames(typeof(GitBackend));

            ReadSettings();
            CancelButton.IsEnabled = false;
        }

        private static Encoding GetSystemDefaultEncoding()
        {
            var ansiCodePage = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage;
            return Encoding.GetEncoding(ansiCodePage);
        }

        private void GoButton_Click(object? sender, RoutedEventArgs e)
        {
            if (!workQueue.IsIdle)
            {
                if (workQueue.IsSuspended)
                {
                    orchestrator?.Resume();
                }
                else
                {
                    orchestrator?.Pause();
                }
                return;
            }

            WriteSettings();

            var encoding = Encoding.Default;
            if (codePages.TryGetValue(EncodingComboBox.SelectedIndex, out var encodingInfo))
            {
                encoding = encodingInfo.GetEncoding();
            }

            var config = SettingsMapper.FromSettings(AppSettings.Load(), encoding);
            config.IgnoreErrors = IgnoreErrorsCheckBox.IsChecked ?? false;
            config.FromDate = (FromDateCheckBox.IsChecked ?? false) ? FromDatePicker.SelectedDate?.Date : null;
            config.ToDate = (ToDateCheckBox.IsChecked ?? false) ? ToDatePicker.SelectedDate?.Date : null;

            var userInteraction = new AvaloniaUserInteraction(this);
            var statusReporter = new AvaloniaStatusReporter(statusTimer);

            Logger.RotateLogFile(config.LogFile);

            orchestrator = new MigrationOrchestrator(config, workQueue, userInteraction, statusReporter);
            orchestrator.Run();
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            workQueue.Abort();
        }

        private void StatusTimer_Tick(object? sender, EventArgs e)
        {
            StatusLabel.Text = workQueue.LastStatus ?? "Idle";
            TimeLabel.Text = $"Elapsed: {new DateTime(workQueue.ActiveTime.Ticks):HH:mm:ss}";

            if (orchestrator?.RevisionAnalyzer != null)
            {
                FileLabel.Text = "Files: " + orchestrator.RevisionAnalyzer.FileCount;
                RevisionLabel.Text = "Revisions: " + orchestrator.RevisionAnalyzer.RevisionCount;
            }

            if (orchestrator?.ChangesetBuilder != null)
            {
                ChangeLabel.Text = "Changesets: " + orchestrator.ChangesetBuilder.Changesets.Count;
            }

            if (workQueue.IsIdle)
            {
                orchestrator = null;

                statusTimer.Stop();
                GoButton.Content = "Go";
                GoButton.IsEnabled = true;
                CancelButton.IsEnabled = false;
            }
            else if (workQueue.IsSuspended)
            {
                GoButton.Content = "Resume";
                GoButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
            }
            else
            {
                var status = workQueue.LastStatus ?? "";
                var isGitExportPhase = status.Contains("Replaying") || status.Contains("Committing") ||
                                        status.Contains("tag") || status.Contains("Initializing Git");

                if (isGitExportPhase)
                {
                    GoButton.Content = "Pause";
                    GoButton.IsEnabled = true;
                }
                else
                {
                    GoButton.Content = "Running...";
                    GoButton.IsEnabled = false;
                }
                CancelButton.IsEnabled = true;
            }

            var exceptions = workQueue.FetchExceptions();
            if (exceptions != null)
            {
                foreach (var exception in exceptions)
                {
                    ShowException(exception);
                }
            }
        }

        private void ShowException(Exception exception)
        {
            var message = ExceptionFormatter.Format(exception);
            logger.WriteLine("ERROR: {0}", message);
            logger.WriteLine(exception);

            MessageDialog.Show(this, "Unhandled Exception", message, "OK");
        }

        private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
        {
            if (!workQueue.IsIdle)
            {
                var confirmed = MessageDialog.Show(this, "Migration in Progress",
                    "VSS to Git migration is currently running. Do you really want to quit?", "Yes", "No") == "Yes";

                if (!confirmed)
                {
                    e.Cancel = true;
                    return;
                }
            }

            WriteSettings();

            workQueue.Abort();
            workQueue.WaitIdle();
        }

        private void ReadSettings()
        {
            var settings = AppSettings.Load();
            VssDirTextBox.Text = settings.VssDirectory;
            VssProjectTextBox.Text = settings.VssProject;
            ExcludeTextBox.Text = settings.VssExcludePaths;
            OutDirTextBox.Text = settings.GitDirectory;
            DomainTextBox.Text = settings.DefaultEmailDomain;
            CommentTextBox.Text = settings.DefaultComment;
            LogTextBox.Text = settings.LogFile;
            TranscodeCheckBox.IsChecked = settings.TranscodeComments;
            ForceAnnotatedCheckBox.IsChecked = settings.ForceAnnotatedTags;
            ExportProjectToGitRootCheckBox.IsChecked = settings.ExportProjectToGitRoot;
            AnyCommentUpDown.Value = settings.AnyCommentSeconds;
            SameCommentUpDown.Value = settings.SameCommentSeconds;

            var backendIndex = Array.IndexOf(Enum.GetNames(typeof(GitBackend)), settings.GitBackend);
            GitBackendComboBox.SelectedIndex = backendIndex >= 0 ? backendIndex : 0;

            if (DateTime.TryParse(settings.FromDate, out var fromDate))
            {
                FromDateCheckBox.IsChecked = true;
                FromDatePicker.SelectedDate = fromDate;
            }
            if (DateTime.TryParse(settings.ToDate, out var toDate))
            {
                ToDateCheckBox.IsChecked = true;
                ToDatePicker.SelectedDate = toDate;
            }
        }

        private void WriteSettings()
        {
            var settings = AppSettings.Load();
            settings.VssDirectory = VssDirTextBox.Text ?? "";
            settings.VssProject = VssProjectTextBox.Text ?? "";
            settings.VssExcludePaths = ExcludeTextBox.Text ?? "";
            settings.GitDirectory = OutDirTextBox.Text ?? "";
            settings.DefaultEmailDomain = DomainTextBox.Text ?? "";
            settings.DefaultComment = CommentTextBox.Text ?? "";
            settings.LogFile = LogTextBox.Text ?? "";
            settings.TranscodeComments = TranscodeCheckBox.IsChecked ?? false;
            settings.ForceAnnotatedTags = ForceAnnotatedCheckBox.IsChecked ?? false;
            settings.ExportProjectToGitRoot = ExportProjectToGitRootCheckBox.IsChecked ?? false;
            settings.AnyCommentSeconds = (int)(AnyCommentUpDown.Value ?? 0);
            settings.SameCommentSeconds = (int)(SameCommentUpDown.Value ?? 60);
            settings.GitBackend = GitBackendComboBox.SelectedItem?.ToString() ?? "Process";
            settings.FromDate = (FromDateCheckBox.IsChecked ?? false)
                ? FromDatePicker.SelectedDate?.Date.ToString("yyyy-MM-dd") ?? "" : "";
            settings.ToDate = (ToDateCheckBox.IsChecked ?? false)
                ? ToDatePicker.SelectedDate?.Date.ToString("yyyy-MM-dd") ?? "" : "";
            settings.Save();
        }

        private async void BrowseForFolder(TextBox textBox, string title)
        {
            var startLocation = !string.IsNullOrWhiteSpace(textBox.Text) && Directory.Exists(textBox.Text)
                ? await StorageProvider.TryGetFolderFromPathAsync(textBox.Text)
                : null;

            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = title,
                SuggestedStartLocation = startLocation,
                AllowMultiple = false
            });

            if (folders.Count > 0 && folders[0].TryGetLocalPath() is { } path)
            {
                textBox.Text = path;
            }
        }

        private void VssDirBrowseButton_Click(object? sender, RoutedEventArgs e)
        {
            BrowseForFolder(VssDirTextBox, "Select VSS Database Directory");
        }

        private void OutDirBrowseButton_Click(object? sender, RoutedEventArgs e)
        {
            BrowseForFolder(OutDirTextBox, "Select Git Output Directory");
        }

        private void FromDateCheckBox_IsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            FromDatePicker.IsEnabled = FromDateCheckBox.IsChecked ?? false;
        }

        private void ToDateCheckBox_IsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            ToDatePicker.IsEnabled = ToDateCheckBox.IsChecked ?? false;
        }
    }
}
