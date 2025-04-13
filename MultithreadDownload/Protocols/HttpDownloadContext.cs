using MultithreadDownload.Core;
using MultithreadDownload.Utils;

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

        public Result<bool> IsPropertiesVaild()
        {
            // Check if the target path is valid
            if (string.IsNullOrEmpty(TargetPath))
            {
                return Result<bool>.Failure("Target path is not valid.");
            }
            // Check if the URL is valid
            if (string.IsNullOrEmpty(Url) || !(HttpNetworkHelper.LinkCanConnectionAsync(this.Url).Result))
            {
                return Result<bool>.Failure("URL is not valid.");
            }
            // Check if the range start and offset are valid
            if (RangeStart < 0 || RangeOffset <= 0)
            {
                return Result<bool>.Failure("Range start or offset is not valid.");
            }
            return Result<bool>.Success(true);
        }

        public void SetCompletedSize(long size)
        {
            this.CompletedSize = size;
        }
    }
}