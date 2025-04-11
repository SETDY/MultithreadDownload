using MultithreadDownload.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Protocols
{
    /// <summary>
    /// Represents the context for an HTTP download.
    /// </summary>
    public class HttpDownloadContext : IDownloadContext
    {
        /// <summary>
        /// The target path where the downloaded file will be saved.
        /// </summary>
        public string TargetPath { get; set; }

        /// <summary>
        /// The starting byte range for the download.
        /// </summary>
        public long RangeStart { get; set; }

        /// <summary>
        /// The offset for the byte range for the download.
        /// </summary>
        public long RangeOffset { get; internal set; }

        /// <summary>
        /// The ending byte range for the download.
        /// </summary>
        public long RangeEnd
        {
            get
            {
                return RangeStart + RangeOffset;
            }
        }

        /// <summary>
        /// The size of the file has been downloaded.
        /// </summary>
        public long CompletedSize { get; private set; }

        /// <summary>
        /// The URL of the file to be downloaded.
        /// </summary>
        public string Url { get; set; }

        public HttpDownloadContext(string targetPath, long rangeStart, long rangeOffset, string url)
        {
            // Initialize the properties
            this.TargetPath = targetPath;
            this.RangeStart = rangeStart;
            this.RangeOffset = rangeOffset;
            this.Url = url;
            this.CompletedSize = 0;
        }

        public void SetCompletedSize(long size)
        {
            this.CompletedSize = size;
        }
    }
}
