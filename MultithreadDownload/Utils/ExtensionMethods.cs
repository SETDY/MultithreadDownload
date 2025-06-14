using MultithreadDownload.Core.Errors;
using MultithreadDownload.Logging;
using System;
using System.IO;

namespace MultithreadDownload.Primitives
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Convert the download speed from bytes to a human-readable format.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string ToSpeed(this long downloadSpeedAsBytes)
        {
            // Convert the download speed to a Gib/s
            if (downloadSpeedAsBytes >= 1024 * 1024 * 1024)
            {
                return $"{Math.Round((double)downloadSpeedAsBytes / (1024 * 1024 * 1024), 2)} GiB/s";
            }
            // Convert the download speed to a Mib/s
            else if (downloadSpeedAsBytes >= 1024 * 1024)
            {
                return $"{Math.Round((double)downloadSpeedAsBytes / (1024 * 1024), 2)} MiB/s";
            }
            // Convert the download speed to a Kib/s
            else if (downloadSpeedAsBytes >= 1024)
            {
                return $"{Math.Round((double)downloadSpeedAsBytes / 1024, 2)} KiB/s";
            }
            else if (downloadSpeedAsBytes >= 0)
            {
                return $"{downloadSpeedAsBytes} B/s";
            }
            throw new ArgumentOutOfRangeException("It is not a vaild input to convert it to a proper speed rate.");
        }

        /// <summary>
        /// Clean up the stream by closing and disposing it, handling exceptions appropriately.
        /// </summary>
        /// <param name="stream">The stream to clean up.</param>
        /// <returns>Whether the cleanup was successful or not.</returns>
        public static Result<bool, DownloadError> CleanUp(this Stream stream)
        {
            try
            {
                if (stream == null) 
                    return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.NullReference, "It is impossible to clean up a null stream."));
                stream.Close();
                stream.Dispose();
                return Result<bool, DownloadError>.Success(true);
            }
            catch (IOException ioEx)
            {
                // Return a specific error for IO issues and log the IOException at the same time
                DownloadLogger.LogError("An error occurred while cleaning up the stream.", ioEx);
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.OutputStreamUnavailable, 
                    $"An IO error occurred while cleaning up the stream. Message: {ioEx.Message}"));
            }
            catch (UnauthorizedAccessException uaEx)
            {
                // Return a specific error for permission issues and log the UnauthorizedAccessException at the same time
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.PermissionDenied, 
                    $"Permission denied while cleaning up the stream. Message: {uaEx.Message}"));
            }
            catch (Exception ex)
            {
                // Return a specific error for unknown issues and log the unknown at the same time
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.UnexpectedOrUnknownException, 
                    $"An unexpected error occurred while cleaning up the stream. Message: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get the category of the download error code.
        /// </summary>
        /// <param name="code">The download error code.</param>
        /// <returns>The category of the download error code.</returns>
        public static DownloadErrorCategory GetCategory(this DownloadErrorCode code) => code switch
        {
            // No error, indicating a successful operation
            DownloadErrorCode.None => DownloadErrorCategory.None,

            // Network-related errors
            DownloadErrorCode.NetworkUnavailable or
            DownloadErrorCode.DnsResolutionFailed or
            DownloadErrorCode.Timeout or
            DownloadErrorCode.InvalidUrl or
            DownloadErrorCode.HttpError =>
                DownloadErrorCategory.Network,

            // Local I/O issues
            DownloadErrorCode.DiskFull or
            DownloadErrorCode.DiskOperationFailed or
            DownloadErrorCode.PermissionDenied or
            DownloadErrorCode.FileAlreadyExists or
            DownloadErrorCode.OutputStreamUnavailable or
            DownloadErrorCode.PathNotFound =>
                DownloadErrorCategory.FileSystem,

            // Protocol or transport layer issues
            DownloadErrorCode.ProtocolNotSupported or
            DownloadErrorCode.RangeNotSatisfiable or
            DownloadErrorCode.ChecksumMismatch =>
                DownloadErrorCategory.Protocol,

            // Structural or scheduler errors
            DownloadErrorCode.ThreadCreationFailed or
            DownloadErrorCode.OutputStreamCountMismatch or
            DownloadErrorCode.ThreadMaxExceeded or
            DownloadErrorCode.SchedulerUnavailable or
            DownloadErrorCode.TaskAlreadyExists or
            DownloadErrorCode.TaskAlreadyStarted or
            DownloadErrorCode.TaskContextInvalid =>
                DownloadErrorCategory.Internal,

            // Last resort for unexpected or unknown errors
            DownloadErrorCode.UnexpectedOrUnknownException or
            DownloadErrorCode.ArgumentOutOfRange or
            DownloadErrorCode.NullReference =>
                DownloadErrorCategory.Unexpected,

            // Handle any undefined or future enum values
            // This case is impossible to reach, but included for no warning to be raised
            _ => throw new ArgumentOutOfRangeException(nameof(code), $"Unhandled DownloadErrorCode: {code}")
        };
    }
}