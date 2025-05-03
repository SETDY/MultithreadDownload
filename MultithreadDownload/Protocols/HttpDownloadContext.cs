using MultithreadDownload.Utils;
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
        /// The download range for each download thread.
        /// </summary>
        /// <remarks>
        /// rangePosition[n][0] = startPostion; rangePosition[n][1] = endPosition
        /// </remarks>
        public long[,] RangePositions { get; private set; }

        /// <summary>
        /// The size of the file has been downloaded.
        /// </summary>
        public long CompletedSize { get; private set; }

        /// <summary>
        /// The URL of the file to be downloaded.
        /// </summary>
        public string Url { get; set; }

        public HttpDownloadContext(string targetPath, string url, long[,] rangePositions)
        {
            // Initialize the properties
            this.TargetPath = targetPath;
            this.RangePositions = rangePositions;
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
            if (this.RangePositions == null)
            {
                return Result<bool>.Failure("Range position cannot be null");
            }
            return Result<bool>.Success(true);
        }

        public void SetCompletedSize(long size)
        {
            this.CompletedSize = size;
        }

        /// <summary>
        /// Creates a new instance of the HttpDownloadContext class.
        /// </summary>
        /// <param name="maxParallelThreads">The maximum number of parallel threads for downloading.</param>
        /// <param name="targetPath">The target path where the downloaded file will be saved.</param>
        /// <param name="link">The URL of the file to be downloaded.</param>
        /// <returns>The download context.</returns>
        /// <remarks>
        /// Since the file size is not known in advance, it can be zero or a exception can be thrown in the process of getting the file size.
        /// Therefore, <see cref="GetDownloadContext"/> method returns a <see cref="Result{T}"/> object
        /// to indicate success or failure and remind the caller to check the result.
        /// </remarks>
        public static async Task<Result<HttpDownloadContext>> GetDownloadContext(byte maxParallelThreads, string targetPath, string link)
        {
            Result<long> fileSize = await HttpNetworkHelper.GetLinkFileSizeAsync(link);
            if (!fileSize.IsSuccess) { return Result<HttpDownloadContext>.Failure($"Cannot get file size from {link}"); }

            // Since GetFileSegments() method requires a file size greater than 0,
            // this if case is used to handle the case where the file size is 0
            if (fileSize.Value == 0)
            {
                return Result<HttpDownloadContext>.Success(
                    new HttpDownloadContext(targetPath, link, new long[,] { { 0, 0 } }));
            }
            // Get download size for each download thread
            Result<long[,]> segmentRanges = FileSegmentHelper.GetFileSegments(fileSize.Value, maxParallelThreads);
            if (!segmentRanges.IsSuccess)
            {
                Result<HttpDownloadContext>.Failure($"Failed to get file segments. Message:{segmentRanges.ErrorMessage}");
            }
            return Result<HttpDownloadContext>.Success(
                new HttpDownloadContext(targetPath, link, segmentRanges.Value));
        }
    }
}