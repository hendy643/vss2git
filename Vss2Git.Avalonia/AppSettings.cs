using System;
using System.IO;
using System.Text.Json;

namespace Hpdi.Vss2Git.Avalonia
{
    /// <summary>
    /// Minimal JSON-backed replacement for the WinForms app's Properties.Settings,
    /// since that generated class isn't available (or portable) outside the WinForms project.
    /// </summary>
    public class AppSettings
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create),
            "Vss2Git", "avalonia-settings.json");

        public string VssDirectory { get; set; } = "";
        public string VssProject { get; set; } = "$";
        public string VssExcludePaths { get; set; } = "";
        public string GitDirectory { get; set; } = "";
        public string DefaultEmailDomain { get; set; } = "localhost";
        public string DefaultComment { get; set; } = "";
        public string LogFile { get; set; } = "";
        public bool TranscodeComments { get; set; } = true;
        public bool ForceAnnotatedTags { get; set; } = true;
        public bool ExportProjectToGitRoot { get; set; }
        public int AnyCommentSeconds { get; set; }
        public int SameCommentSeconds { get; set; } = 60;
        public string GitBackend { get; set; } = "Process";
        public string FromDate { get; set; } = "";
        public string ToDate { get; set; } = "";

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception)
            {
                // corrupt or unreadable settings file: fall back to defaults
            }
            return new AppSettings();
        }

        public void Save()
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }
}
