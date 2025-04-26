using MultithreadDownload.IO;
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
        public long[][] RangePositions { get; private set; }

        /// <summary>
        /// The size of the file has been downloaded.
        /// </summary>
        public long CompletedSize { get; private set; }

        /// <summary>
        /// The URL of the file to be downloaded.
        /// </summary>
        public string Url { get; set; }

        public HttpDownloadContext(string targetPath, string url, long[][] rangePositions)
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

        public static async Task<HttpDownloadContext> GetDownloadContext(byte maxParallelThreads, string targetPath, string link)
        {
            Result<long> fileSize = await HttpNetworkHelper.GetLinkFileSizeAsync(link);
            if (!fileSize.IsSuccess) { return null; }
            Result<long> rangeStart = FileSegmentHelper.SplitSize
                (maxParallelThreads, fileSize.Value, out Result<long> remainingSize);
            if (!rangeStart.IsSuccess) { return null; }
            // Get download size for each download thread
            long[][] rangePosition = GetRangePositions(maxParallelThreads, fileSize.Value, remainingSize.Value);
            return new HttpDownloadContext(targetPath, link, rangePosition);
        }

        /// <summary>
        /// Get the range position for each thread.
        /// </summary>
        /// <param name="maxParallelThreads">The maximum number of parallel threads.</param>
        /// <param name="fileSize">The size of the file to be downloaded.</param>
        /// <param name="remainingSize">The remaining size of the file after dividing it into ranges.</param>
        /// <returns>The range position for each thread.</returns>
            private static long[][] GetRangePositions(byte maxParallelThreads, long fileSize, long remainingSize)
            {
                // Calculate the range positions for each thread
                // e.g. rangePosition[n][0] = startPostion; rangePosition[n][1] = endPosition
                long[][] rangePositions = new long[maxParallelThreads][];
                int rangeStart = 0;
                long rangeOffset = fileSize / maxParallelThreads;
                for (int i = 0; i < maxParallelThreads; i++)
                {
                    rangePositions[i][0] = rangeStart + (i * rangeOffset);
                    rangePositions[i][1] = rangeStart + ((i + 1) * rangeOffset) - 1;
                }
                // Set the last thread's end position to the file size
                rangePositions[maxParallelThreads - 1][1] += remainingSize;

                return rangePositions;
            }
    }
}