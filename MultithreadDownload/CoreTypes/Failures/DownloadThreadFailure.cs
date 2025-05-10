using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.CoreTypes.Failures
{
    /// <summary>
    /// Represent the failure reasons for the download thread.
    /// </summary>
    public enum DownloadThreadFailureReason
    {
        /// <summary>
        /// The number of threads has reached the maximum allowed.
        /// </summary>
        ThreadPoolCapacityExceeded
    }

#nullable enable
    /// <summary>
    /// Represents an error that occurred during the download process.
    /// </summary>
    /// <param name="Kind">The reason for the download failure.</param>
    /// <param name="Message">The error message.</param>
    /// <param name="Exception">The exception that caused the error, if any.</param>
    public record DownloadThreadFailure(DownloadThreadFailureReason Kind, string? Message = null, Exception? Exception = null);
#nullable disable
}
