using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.CoreTypes.Failures
{
    /// <summary>
    /// Represents the failure reasons for the download process step.
    /// </summary>
    public enum DownloadProcessFailureReason
    {
        /// <summary>
        /// The file already exists at the target path.
        /// </summary>
        FileAlreadyExisted,
        /// <summary>
        /// An I/O error occurred during the download process.
        /// </summary>
        IOFailure,
        /// <summary>
        /// Unauthorized access to the file or directory.
        /// </summary>
        UnauthorisedAccessFailure,
        /// <summary>
        /// An unknown stream failure occurred.
        /// </summary>
        UnknownStreamFailure,
    }

#nullable enable
    /// <summary>
    /// Represents an failure that occurred during the download process.
    /// </summary>
    /// <param name="Kind">The reason for the download failure.</param>
    /// <param name="Message">The failure message.</param>
    /// <param name="Exception">The exception that caused the error, if any.</param>
    public record DownloadProcessFailure(DownloadProcessFailureReason Kind, string? Message = null, Exception? Exception = null);
#nullable disable
}
