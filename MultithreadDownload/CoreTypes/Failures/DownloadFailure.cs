using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.CoreTypes.Failures
{
    /// <summary>
    /// Represents the reasons for download failure.
    /// </summary>
    /// <example>
    /// Result<![CDATA[<]]>long, DownloadFailureReason> result = GetDownloadSize(string url);
    /// if (!result.IsSuccess)
    /// {
    ///     switch (result.FailureReason)
    ///     {
    ///         case DownloadFailureReason.InvalidUrl:
    ///             Console.WriteLine("Invalid URL.");
    ///             break;
    ///         case DownloadFailureReason.ConnectionFailed:
    ///             Console.WriteLine("Connection failed.");
    ///             break;
    ///          ...
    /// </example>
    public enum DownloadFailureReason
    {
        /// <summary>
        /// The url is invalid or empty.
        /// </summary>
        InvalidUrl,
        /// <summary>
        /// The server returned an unexpected response for the file size.
        /// </summary>
        CannotGetFileSize,
        /// <summary>
        /// The path process failed.
        /// </summary>
        PathProcessFailure,
        /// <summary>
        /// The time of connection to the server is over the limit.
        /// </summary>
        ConnectionTimeout,
        /// <summary>
        /// Cannot write the data to the file.
        /// </summary>
        WriteFailure,
        /// <summary>
        /// An failure occurred unexpectedly.
        /// </summary>
        UnexpectedFailure
    }

#nullable enable
    /// <summary>
    /// Represents an failure that occurred during the download process.
    /// </summary>
    /// <param name="Kind">The reason for the download failure.</param>
    /// <param name="Message">The failure message.</param>
    /// <param name="Exception">The exception that caused the error, if any.</param>
    public record DownloadFailure(DownloadFailureReason Kind, string? Message = null, Exception? Exception = null);
#nullable disable
}
