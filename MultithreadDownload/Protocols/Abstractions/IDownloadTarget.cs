using MultithreadDownload.Core.Errors;
using MultithreadDownload.Primitives;
using MultithreadDownload.Utils.IO;
using System;

namespace MultithreadDownload.Protocols.Abstractions
{
    /// <summary>
    /// Interface for download data that is used for creating a download task.
    /// </summary>
    public interface IDownloadTarget
    {
        /// <summary>
        /// The file path where the downloaded file will be saved in.
        /// </summary>
        public FilePath FilePath { get; }

        /// <summary>
        /// The length of the content to be downloaded, in bytes. <br/>
        /// This value may be null if the content length is not known or cannot be determined.
        /// </summary>
        public long? ContentLength { get; }

        /// <summary>
        /// Whether the download target supports range requests. 
        /// </summary>
        /// <remarks>
        /// If true, the download can be performed in multiple threads by specifying byte ranges for each thread. <br/>
        /// If false, the download must be performed in a single thread without byte range specifications.
        /// </remarks>
        public bool SupportsRange { get; }
    }
}