using MultithreadDownload.Primitives;
using MultithreadDownload.Tasks.Abstractions;
using MultithreadDownload.Utils;
using System;
using System.Diagnostics;
using System.Threading;

namespace MultithreadDownload.Tasks.Trackers
{
    /// <summary>
    /// This class is used to track the download speed of a file.
    /// </summary>
    public class DownloadSpeedTracker : IDownloadSpeedTracker
    {

        private readonly uint MIN_SPEED_REPORT_INTERVAL_IN_MILLISECONDS = 500;

        /// <summary>
        /// The stopwatch used to measure elapsed time.
        /// </summary>
        private readonly Stopwatch _stopwatch = new Stopwatch();

        /// <summary>
        /// A lock object to ensure thread safety when accessing shared resources.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// Timer for invoking the speed update event at regular intervals.
        /// </summary>
        private Timer _timer;

        /// <summary>
        /// The last recorded time in milliseconds.
        /// </summary>
        private TimeSpan _lastRecordedTime;

        /// <summary>
        /// The last number of bytes recorded since the last change in bytes calculation.
        /// </summary>
        private long _lastBytes;

        /// <summary>
        /// The total number of bytes downloaded so far.
        /// </summary>
        private long _totalBytes;

        /// <summary>
        /// The last formatted speed information
        /// </summary>
        /// <remarks>
        /// It can be used to optimize speed reporting 
        /// by avoiding redundant calculations if the speed hasn't changed significantly since the last report.
        /// </remarks>
        private FormattedSpeed lastFormattedSpeed;

#nullable enable

        /// <summary>
        /// An event that is triggered with a regular interval to report the formatted download speed when StartMonitoring() is called.
        /// </summary>
        [Obsolete("Use FormattedSpeedReportGenerated instead.", false)]
        public event Action<string>? SpeedReportGenerated;

        /// <summary>
        /// Occurs when a new formatted speed report is generated.
        /// </summary>
        /// <remarks>
        /// Subscribers receive a FormattedSpeed instance containing the latest speed information. 
        /// This event is typically raised after speed data has been processed and formatted for display or
        /// logging purposes.
        /// </remarks>
        public event Action<FormattedSpeed>? FormattedSpeedReportGenerated;

#nullable disable

        public SpeedFormatOption SpeedFormatOption { get; init; }

        /// <summary>
        /// The download speed tracker's elapsed time span, which represents the total time that has passed since the tracker was started.
        /// </summary>
        public TimeSpan ElapsedTimeSpan => this._stopwatch.Elapsed;

        public DownloadSpeedTracker() : this(SpeedFormatOption.CreateOption()) { }

        public DownloadSpeedTracker(SpeedFormatOption formatOption)
        {
            // Set the speed format option.
            this.SpeedFormatOption = formatOption;
            // Start the stopwatch to begin tracking time.
            _stopwatch.Start();
        }

        /// <summary>
        /// Starts monitoring the download speed at a specified interval.
        /// </summary>
        /// <param name="interval"></param>
        public void StartMonitoring(TimeSpan interval)
        {
            // If the timer is already running, do not start another one to avoid multiple timers running simultaneously.
            if (_timer != null) return;

            // Create a new timer that triggers the SpeedReportGenerated event at the specified interval,
            // passing the formatted speed as an argument.
            _timer = new Timer(_ =>
            {
                // Get the formatted speed information based on the specified data unit type and
                // invoke the FormattedSpeedReportGenerated event with the new speed data.
                this.FormattedSpeedReportGenerated?.Invoke(GetFormattedSpeedInfo());

                // OBSOLETE: This section of code is marked as obsolete and waits for removal in future versions.
                // PS:  Older code that reports the speed as a string, which still exists for backward compatibility
                //      but is marked as obsolete.
                //      It retrieves the formatted speed string and invokes the SpeedReportGenerated event.
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
                // Get the time difference in milliseconds since the last speed calculation.
                int deltaTimeInMilliseconds = GetDeltaTimeSpan().Milliseconds;

                // If the time difference is greater than or equal to the minimum speed report interval, calculate the new speed information.
                // Otherwise, return the last calculated speed to avoid unnecessary calculations and provide a more stable speed reading.
                FormattedSpeed finalSpeed = lastFormattedSpeed;
                if (this.MIN_SPEED_REPORT_INTERVAL_IN_MILLISECONDS >= 500)
                {
                    finalSpeed = GetFormattedSpeedInfo();
                }
                return finalSpeed.WithFormat(SpeedFormatOption.CreateOption(
                    new DataUnitInfo(DataUnitType.Basic, this.SpeedFormatOption.DataUnitInfo.Style),
                    this.SpeedFormatOption.TimeUnitInfo,
                    this.SpeedFormatOption.DecimalPlaces
                    )).DeltaBytesPerUnitTime;
            }
        }

        /// <summary>
        /// Gets the formatted speed information using the specified data unit type.
        /// </summary>
        /// <returns>
        /// A FormattedSpeed object containing the calculated speed and its formatted representation based on the
        /// specified unit type.
        /// </returns>
        public FormattedSpeed GetFormattedSpeedInfo()
        {
            // Use lock to ensure thread safety when accessing shared resources.
            lock (_lock)
            {
                return FormattedSpeed.Format(this.GetDeltaBytes(), _stopwatch.Elapsed - _lastRecordedTime, this.SpeedFormatOption);
            }
        }

        /// <summary>
        /// Gets the current download speed formatted as a string with appropriate units (e.g., KB/s, MB/s).
        /// </summary>
        /// <returns>The formatted download speed string.</returns>
        [Obsolete("Use GetFormattedSpeedInfo() which returns structured speed data.", false)]
        public string GetSpeedFormatted()
        {
            // In fact, GetSpeedInBytesPerSecond() never returns a REAL decimal value,
            // it always returns a long value as the download speed is calculated based on the total bytes and elapsed time,
            // which are both integers.
            // However, as the WRONG defition of the method requires a double return type which is written in the interface,
            // we have to treat the returned value as a double, even though it is essentially a long value representing the download speed in bytes per second.
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

        /// <summary>
        /// Calculates the elapsed time since the last measurement using the internal stopwatch.
        /// </summary>
        /// <remarks>
        /// This method updates the internal state to ensure subsequent calls return the time
        /// interval since the last invocation.
        /// </remarks>
        /// <returns>
        /// A TimeSpan representing the duration between the current and previous stopwatch readings.
        /// </returns>
        private TimeSpan GetDeltaTimeSpan()
        {
            TimeSpan deltaTimeSpan = this._stopwatch.Elapsed - this._lastRecordedTime;
            this._lastRecordedTime = this._stopwatch.Elapsed;
            return deltaTimeSpan;
        }

        /// <summary>
        /// Calculates the difference in bytes since the last recorded value.
        /// </summary>
        /// <remarks>
        /// This method updates the internal state to ensure subsequent calls return the change in bytes since the last measurement. 
        /// It calculates the delta by subtracting the last recorded bytes from the total bytes, 
        /// and then updates the last recorded bytes to the current total for the next calculation. 
        /// This allows for accurate tracking of byte changes over time, 
        /// which is essential for calculating download speed based on the time.
        /// </remarks>
        /// <returns>
        /// A TimeSpan representing the delta in bytes between the current and previous measurements.
        /// </returns>
        private long GetDeltaBytes()
        {
            long deltaBytes = this._lastBytes - this._lastBytes;
            this._lastBytes = this._totalBytes;
            return deltaBytes;
        }
    }
}
