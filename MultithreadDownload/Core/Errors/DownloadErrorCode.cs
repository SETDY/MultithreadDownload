using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Core.Errors
{
    /// <summary>
    /// Represents error codes that can occur during the download process.
    /// </summary>
    /// <remarks>
    /// For each error code, there is a corresponding <see cref="DownloadErrorCategory"/> that categorizes the error.
    /// Therefore, for every changes made to this enum, the <see cref="DownloadErrorCode"/>.GetCategory() should also be updated accordingly.
    /// </remarks>
    public enum DownloadErrorCode
    {
        // Represents no error, indicating a successful operation.
        None = 0,

        // Network-related errors that can occur during the download process.
        NetworkUnavailable,
        Timeout,
        InvalidUrl,
        HttpError,
        DnsResolutionFailed,

        // FileSystem-related errors that can occur when accessing the local file system.
        DiskFull,
        DiskOperationFailed,
        PermissionDenied,
        FileAlreadyExists,
        PathNotFound,
        OutputStreamUnavailable,

        // Task-related errors that can occur during the download process.
        SchedulerUnavailable,
        TaskAlreadyExists,
        TaskAlreadyStarted,
        TaskContextInvalid,
        ThreadMaxExceeded,
        ThreadCreationFailed,
        OutputStreamCountMismatch,

        // Protocol-related errors that can occur during the download process.
        ProtocolNotSupported,
        RangeNotSatisfiable,
        ChecksumMismatch,

        // GeneralError.
        NullReference,
        ArgumentOutOfRange,
        UnexpectedOrUnknownException,
    }

}
