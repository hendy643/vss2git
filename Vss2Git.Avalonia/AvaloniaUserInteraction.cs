using System;
using Avalonia.Controls;

namespace Hpdi.Vss2Git.Avalonia
{
    /// <summary>
    /// Dialog-based implementation of user interaction for the Avalonia GUI.
    /// </summary>
    public class AvaloniaUserInteraction : IUserInteraction
    {
        private readonly Window? owner;

        public AvaloniaUserInteraction(Window? owner = null)
        {
            this.owner = owner;
        }

        public ErrorAction ReportError(string message, ErrorActionOptions options)
        {
            var buttons = options == ErrorActionOptions.RetryCancel
                ? new[] { "Retry", "Cancel" }
                : new[] { "Abort", "Retry", "Ignore" };

            var result = MessageDialog.Show(owner, "Error", message, buttons);

            return result switch
            {
                "Retry" => ErrorAction.Retry,
                "Ignore" => ErrorAction.Ignore,
                _ => ErrorAction.Abort
            };
        }

        public bool Confirm(string message, string title)
        {
            return MessageDialog.Show(owner, title, message, "Yes", "No") == "Yes";
        }

        public void ShowFatalError(string message, Exception? exception)
        {
            var fullMessage = exception != null
                ? $"{message}\n\n{ExceptionFormatter.Format(exception)}"
                : message;

            MessageDialog.Show(owner, "Error", fullMessage, "OK");
        }
    }
}
