using MultithreadDownload.Logging;
using MultithreadDownload.Utils;
using System;
using System.Threading;

namespace MultithreadDownload.Tasks
{
    /// <summary>
    /// A class that monitors the download speed.
    /// </summary>
    public class DownloadSpeedMonitor : IDownloadSpeedMonitor, IDisposable
    {
        /// <summary>
        /// Timer for monitoring download speed which is set to 1 second interval.
        /// </summary>
        private Timer _timer;

        /// <summary>
        /// The last timestamp when the speed was calculated.
        /// </summary>
        /// <remarks>
        /// Since the timer has errors for every 1 second, we need to use a timestamp to calculate the speed.
        /// </remarks>
        private DateTime _lastTimestamp;

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
            // Check if the timer is already initialized to prevent creating multiple timers
            if (_timer != null)
            {
                // Log the warning if the timer is already initialized
                DownloadLogger.LogWarning("The timer is already initialized. Please stop the timer before starting it again.");
                return;
            }

            // Initialize properties
            _lastTimestamp = DateTime.Now;
            _getDownloadedSize = getDownloadedSize;
            _lastDownloadedSize = _getDownloadedSize.Invoke();

            // Set the timer to update every second
            // Get the current size and calculate the speed
            // Convert to proper unit and invoke the event
            _timer = new Timer(_ =>
            {
                // Log the execution of the timer
                //DownloadLogger.LogInfo($"Timer executed and the _isTimerStopped is {_isTimerStopped}");

                // Check if the timer is stopped
                if (_isTimerStopped) return;
                // Otherwise, get the current size and get the difference of time
                long currentSize = _getDownloadedSize.Invoke();
                // There is a problem with the timer if the file downloads fastly which causes the speed is overated.
                // So we need to use a timestamp as the difference of time to calculate the speed
                double seconds = (DateTime.Now - _lastTimestamp).TotalSeconds;
                string downloadSpeed = ((long)((currentSize - _lastDownloadedSize) / seconds)).ToSpeed();
                // After culculating the speed, we need to update the timestamp and the last downloaded size
                _lastTimestamp = DateTime.Now;
                _lastDownloadedSize = currentSize;
                // Invoke the event with the calculated speed
                OnSpeedUpdated?.Invoke(downloadSpeed);

                // Log the download speed
                DownloadLogger.LogInfo($"The download speed is {downloadSpeed}");
            }, null, 1000, 1000);
        }

        /// <summary>
        /// Stops the speed monitor but does not dispose the timer.
        /// </summary>
        public void Stop()
        {
            _isTimerStopped = true;
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
        /// <summary>
        /// Disposes the speed monitor and the timer.
        /// </summary>
        public void Dispose()
        {
            // If the timer is not stopped, stop it
            if (!_isTimerStopped)
                Stop();
            // Dispose the timer if it is not null
            _timer?.Dispose();
            // Log the disposal of the timer
            DownloadLogger.LogInfo("The timer is disposed.");
        }
    }
}