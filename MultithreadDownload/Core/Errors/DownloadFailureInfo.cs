using System;
using System.Collections.Generic;
using System.Text;

namespace MultithreadDownload.Core.Errors
{
    /// <summary>
    /// Represents the information about a download failure, including the error details and the remaining bytes that were not downloaded due to the failure.
    /// </summary>
    public struct DownloadFailureInfo
    {
        /// <summary>
        /// The error details of the download failure, which includes the error code, category, and a readable message describing the error.
        /// </summary>
        public DownloadError Error { get; init; }

        /// <summary>
        /// The number of bytes that were not downloaded due to the failure
        /// </summary>
        public long RemainingBytes { get; init; }

        public DownloadFailureInfo(DownloadError error, long remainingBytes)
        {
            this.Error = error;
            this.RemainingBytes = remainingBytes;
        }
    }
}
