using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Tasks
{
    public interface IDownloadSpeedTracker : IDisposable
    {
#nullable enable
        /// <summary>
        /// An event that is triggered with a regular interval to report the formatted download speed.
        /// </summary>
        public event Action<string>? SpeedReportGenerated;
#nullable disable

        /// <summary>
        /// Starts monitoring the download speed at a specified interval.
        /// </summary>
        /// <param name="interval"></param>
        public void StartMonitoring(TimeSpan interval);

        /// <summary>
        /// Stops monitoring the download speed by disposing of the timer.
        /// </summary>
        public void StopMonitoring();

        /// <summary>
        /// Reports the number of bytes written to the tracker.
        /// </summary>
        /// <param name="bytesWritten">The number of bytes written.</param>
        public void ReportBytes(long bytesWritten);

        /// <summary>
        /// Gets the current download speed in bytes per second.
        /// </summary>
        /// <returns>The download speed in bytes per second.</returns>
        public double GetSpeedInBytesPerSecond();

        /// <summary>
        /// Gets the current download speed formatted as a string with appropriate units (e.g., KB/s, MB/s).
        /// </summary>
        /// <returns>The formatted download speed string.</returns>
        public string GetSpeedFormatted();
    }
}
