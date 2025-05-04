using MultithreadDownload.Utils;
using System;
using System.Threading;

namespace MultithreadDownload.Tasks
{
    /// <summary>
    /// A class that monitors the download speed.
    /// </summary>
    public class DownloadSpeedMonitor : IDownloadSpeedMonitor
    {
        /// <summary>
        /// Timer for monitoring download speed which is set to 1 second interval.
        /// </summary>
        private Timer s_timer;

        private bool s_isTimerStopped;

        /// <summary>
        /// The last downloaded size.
        /// </summary>
        private long s_lastDownloadedSize;

        /// <summary>
        /// The function to get the downloaded size.
        /// </summary>
        private Func<long> s_getDownloadedSize;

        /// <summary>
        /// An event that is triggered when the download speed is updated.
        /// </summary>
        public event Action<string> SpeedUpdated;

        /// <summary>
        /// Starts the speed monitor.
        /// </summary>
        /// <param name="getDownloadedSize">A method to get the downloaded size.</param>
        public void Start(Func<long> getDownloadedSize)
        {
            // Initialize properties
            this.s_getDownloadedSize = getDownloadedSize;
            this.s_lastDownloadedSize = s_getDownloadedSize.Invoke();

            // Set the timer to update every second
            // Get the current size and calculate the speed
            // Convert to proper unit and invoke the event
            s_timer = new Timer(_ =>
            {
                if (s_isTimerStopped) return;
                long currentSize = s_getDownloadedSize.Invoke();
                string downloadSpeed = ((long)((currentSize - s_lastDownloadedSize) / 1)).ToSpeed();
                s_lastDownloadedSize = currentSize;

                SpeedUpdated?.Invoke(downloadSpeed);
            }, null, 1000, 1000);
        }

        /// <summary>
        /// Stops the speed monitor.
        /// </summary>
        public void Stop()
        {
            s_isTimerStopped = true;
            s_timer?.Dispose();
        }
    }
}