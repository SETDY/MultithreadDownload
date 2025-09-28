using MultithreadDownload.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Tasks
{
    /// <summary>
    /// This class is used to track the download speed of a file.
    /// </summary>
    public class DownloadSpeedTracker : IDownloadSpeedTracker
    {
        /// <summary>
        /// The stopwatch used to measure elapsed time.
        /// </summary>
        private readonly Stopwatch _stopwatch = new();

        /// <summary>
        /// A lock object to ensure thread safety when accessing shared resources.
        /// </summary>
        private readonly object _lock = new();

        /// <summary>
        /// Timer for invoking the speed update event at regular intervals.
        /// </summary>
        private Timer _timer;

        /// <summary>
        /// The last recorded time in milliseconds.
        /// </summary>
        private long _lastRecordedTimeMs;

        /// <summary>
        /// The last number of bytes recorded since the last speed calculation.
        /// </summary>
        private long _lastBytes;

        /// <summary>
        /// The total number of bytes downloaded so far.
        /// </summary>
        private long _totalBytes;

#nullable enable
        /// <summary>
        /// An event that is triggered with a regular interval to report the formatted download speed when StartMonitoring() is called.
        /// </summary>
        /// <remarks>
        /// It is recommended to use this event to update the UI or log the download speed.
        /// </remarks>
        public event Action<string>? SpeedReportGenerated;
#nullable disable

        public DownloadSpeedTracker()
        {
            _stopwatch.Start();
        }

        /// <summary>
        /// Starts monitoring the download speed at a specified interval.
        /// </summary>
        /// <param name="interval"></param>
        public void StartMonitoring(TimeSpan interval)
        {
            if (_timer != null) return; // 防止重复启动
            _timer = new Timer(_ =>
            {
                var speed = GetSpeedFormatted();
                SpeedReportGenerated?.Invoke(speed);
            }, null, interval, interval);
        }

        /// <summary>
        /// Stops monitoring the download speed by disposing of the timer.
        /// </summary>
        public void StopMonitoring()
        {
            _timer?.Dispose();
            _timer = null;
        }

        /// <summary>
        /// Reports the number of bytes written to the tracker.
        /// </summary>
        /// <param name="bytesWritten">The number of bytes written.</param>
        public void ReportBytes(long bytesWritten)
        {
            // Use Interlocked to ensure thread safety when updating the total bytes.
            Interlocked.Add(ref _totalBytes, bytesWritten);
        }

        /// <summary>
        /// Gets the current download speed in bytes per second.
        /// </summary>
        /// <returns>The download speed in bytes per second.</returns>
        public double GetSpeedInBytesPerSecond()
        {
            // Use lock to ensure thread safety when accessing shared resources.
            lock (_lock)
            {
                long nowTimeMoment = _stopwatch.ElapsedMilliseconds;
                var deltaTime = nowTimeMoment - _lastRecordedTimeMs;
                var deltaBytes = _totalBytes - _lastBytes;

                // If the time difference is less than 500 milliseconds, return 0 to avoid rapid fluctuations.
                if (deltaTime < 500) return 0;

                _lastRecordedTimeMs = nowTimeMoment;
                _lastBytes = _totalBytes;

                return deltaBytes / (deltaTime / 1000.0);
            }
        }

        /// <summary>
        /// Gets the current download speed formatted as a string with appropriate units (e.g., KB/s, MB/s).
        /// </summary>
        /// <returns>The formatted download speed string.</returns>
        public string GetSpeedFormatted()
        {
            return ((long)GetSpeedInBytesPerSecond()).ToSpeed();
        }

        /// <summary>
        /// Disposes of the tracker, stopping the stopwatch and resetting it.
        /// </summary>
        public void Dispose()
        {
            // Stop the stopwatch when disposing of the tracker.
            _stopwatch.Stop();
            _stopwatch.Reset();
            // Stop the speed monitor functionality if it is running.
            StopMonitoring();
        }
    }
}
