using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Core.Errors
{
    /// <summary>
    /// Represents the category of errors that can occur during the download process.
    /// </summary>
    /// <remarks>
    /// For each error category, there are specific error codes defined in <see cref="DownloadErrorCode"/>.
    /// </remarks>
    public enum DownloadErrorCategory
    {
        /// <summary>
        /// No error occurred, indicating a successful operation.
        /// </summary>
        None,

        /// <summary>
        /// Network-related errors that can occur during the download process
        /// For example, Network connectivity, DNS, disconnection, and other related errors.
        /// </summary>
        Network,

        /// <summary>
        /// FileSystem-related errors that can occur when accessing the local file system.
        /// For example, disk full, permission denied, file already exists, path not found, output stream unavailable.
        /// </summary>
        FileSystem,

        /// <summary>
        /// Task-related errors that can occur during the download process.
        /// For example, scheduler unavailable, task already exists, task already started, task context invalid, thread max exceeded, thread creation failed, output stream count mismatch.
        /// </summary>
        Internal,

        /// <summary>
        /// Protocol-related errors that can occur during the download process.
        /// For example, protocol not supported, range not satisfiable, checksum mismatch.
        /// </summary>
        Protocol,

        /// <summary>
        /// General errors.
        /// For example, null reference, unexpected or unknown exception.
        /// </summary>
        Unexpected
    }



}
