using Avalonia.Threading;

namespace Hpdi.Vss2Git.Avalonia
{
    /// <summary>
    /// GUI-based status reporter that manages a DispatcherTimer.
    /// </summary>
    public class AvaloniaStatusReporter : IStatusReporter
    {
        private readonly DispatcherTimer statusTimer;

        public AvaloniaStatusReporter(DispatcherTimer statusTimer)
        {
            this.statusTimer = statusTimer;
        }

        public void Start()
        {
            statusTimer.Start();
        }

        public void Stop()
        {
            statusTimer.Stop();
        }
    }
}
