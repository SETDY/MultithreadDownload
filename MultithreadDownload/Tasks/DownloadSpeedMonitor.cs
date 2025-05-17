using MultithreadDownload.Logging;
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
        private Timer _timer;

        private bool _isTimerStopped;

        /// <summary>
        /// The last downloaded size.
        /// </summary>
        private long _lastDownloadedSize;

        /// <summary>
        /// The function to get the downloaded size.
        /// </summary>
        private Func<long> _getDownloadedSize;

        /// <summary>
        /// An event that is triggered when the download speed is updated.
        /// </summary>
        public event Action<string> OnSpeedUpdated;

        /// <summary>
        /// Starts the speed monitor.
        /// </summary>
        /// <param name="getDownloadedSize">A method to get the downloaded size.</param>
        public void Start(Func<long> getDownloadedSize)
        {
            //if (_timer == null)
            //{
            //    // Log the warning if the timer is already initialized
            //    DownloadLogger.LogWarning("The timer is already initialized. Please stop the timer before starting it again.");
            //    return;
            //}
            // Initialize properties
            this._getDownloadedSize = getDownloadedSize;
            this._lastDownloadedSize = _getDownloadedSize.Invoke();

            // Set the timer to update every second
            // Get the current size and calculate the speed
            // Convert to proper unit and invoke the event
            _timer = new Timer(_ =>
            {
                // Log the execution of the timer
                DownloadLogger.LogInfo($"Timer executed and the _isTimerStopped is {_isTimerStopped}");
                if (_isTimerStopped) return;
                long currentSize = _getDownloadedSize.Invoke();
                string downloadSpeed = ((long)((currentSize - _lastDownloadedSize) / 1)).ToSpeed();
                _lastDownloadedSize = currentSize;

                OnSpeedUpdated?.Invoke(downloadSpeed);

                // Log the download speed
                DownloadLogger.LogInfo($"The download speed is {downloadSpeed}");
            }, null, 1000, 1000);
        }

        /// <summary>
        /// Stops the speed monitor.
        /// </summary>
        public void Stop()
        {
            _isTimerStopped = true;
            _timer?.Dispose();
        }
    }
}