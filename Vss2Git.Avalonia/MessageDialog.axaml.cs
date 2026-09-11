using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Hpdi.Vss2Git.Avalonia
{
    /// <summary>
    /// Minimal reusable message box replacement (Avalonia has no built-in MessageBox).
    /// Call <see cref="Show"/> from any thread; it marshals onto the UI thread and blocks
    /// until the user picks a button, matching the synchronous WinForms MessageBox.Show API
    /// that IUserInteraction was written against.
    /// </summary>
    public partial class MessageDialog : Window
    {
        private string? result;

        public MessageDialog()
        {
            InitializeComponent();
        }

        public static string Show(Window? owner, string title, string message, params string[] buttons)
        {
            return Dispatcher.UIThread
                .InvokeAsync(() => ShowOnUiThreadAsync(owner, title, message, buttons))
                .GetAwaiter().GetResult();
        }

        private static async Task<string> ShowOnUiThreadAsync(Window? owner, string title, string message, string[] buttons)
        {
            var dialog = new MessageDialog { Title = title };
            dialog.FindControl<TextBlock>("MessageText")!.Text = message;

            var panel = dialog.FindControl<StackPanel>("ButtonPanel")!;
            foreach (var buttonText in buttons)
            {
                var button = new Button { Content = buttonText, MinWidth = 80 };
                button.Click += (_, _) =>
                {
                    dialog.result = buttonText;
                    dialog.Close();
                };
                panel.Children.Add(button);
            }

            if (owner != null)
            {
                await dialog.ShowDialog(owner);
            }
            else
            {
                var closed = new TaskCompletionSource();
                dialog.Closed += (_, _) => closed.TrySetResult();
                dialog.Show();
                await closed.Task;
            }

            return dialog.result ?? buttons[^1];
        }
    }
}
