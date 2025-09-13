using MultithreadDownload.Core.Errors;
using MultithreadDownload.Primitives;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MultithreadDownload.Protocols.Http
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
        /// The number of threads required for the download.
        /// </summary>
        public byte ThreadCount { get; set; }

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

        public HttpDownloadContext(string targetPath, byte threadCount, string url, long[,] rangePositions)
        {
            // Initialize the properties
            TargetPath = targetPath;
            ThreadCount = threadCount;
            RangePositions = rangePositions;
            Url = url;
            CompletedSize = 0;
        }

        /// <summary>
        /// Checks if the properties of the download context are valid.
        /// </summary>
        /// <returns>The result indicating whether the properties are valid or not.</returns>
        public Result<bool, DownloadError> IsPropertiesVaild()
        {
            // Check if the target path is valid
            if (string.IsNullOrEmpty(TargetPath))
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.PathNotFound, "Target path is not valid."));
            // Check if the URL is valid
            if (string.IsNullOrEmpty(Url) || !HttpNetworkHelper.LinkCanConnectionAsync(Url).Result)
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.InvalidUrl, "URL is not valid."));
            // Check if range positions is nor null
            if (RangePositions == null)
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.RangeNotSatisfiable, "Range position cannot be null"));
            // Check if the range positions count matches the thread count
            if (RangePositions.GetLength(0) != ThreadCount)
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.RangeNotSatisfiable, "Range position count does not match thread count."));
            // Check if the range positions' items is valid
            for (int i = 0; i < ThreadCount; i++)
            {
                // Check if the start position is less than or equal to the end position
                if (RangePositions[i, 0] > RangePositions[i, 1])
                    return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.RangeNotSatisfiable, "Start position cannot be greater than end position."));
                // Check if the start position is less than 0
                if (RangePositions[i, 0] < 0)
                    return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.RangeNotSatisfiable, "Start position cannot be less than 0."));
            }
            return Result<bool, DownloadError>.Success(true);
        }

        public void SetCompletedSize(long size)
        {
            CompletedSize = size;
        }

        /// <summary>
        /// Gets the range size of the file for a thread to downloaded.
        /// </summary>
        /// <returns>The range size of the file in bytes.</returns>
        public long GetRangeSize(byte threadID)
        {
            // Get the range size for the specified thread
            // Get the start and end positions for the thread
            long startPosition = RangePositions[threadID, 0];
            long endPosition = RangePositions[threadID, 1];
            // Calculate and return the range size
            // If the start and end positions are both 0, return 0
            // Otherwise, return the range size
            return startPosition == 0 && endPosition == 0 
                   ? 0 
                   : endPosition - startPosition + 1;
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
        /// Therefore, <see cref="GetDownloadContext"/> method returns a <see cref="Result{T, ErrorCode}"/> object
        /// to indicate success or failure and remind the caller to check the result.
        /// </remarks>
        public static async Task<Result<HttpDownloadContext, DownloadError>> GetDownloadContext(byte maxParallelThreads, string savedPath, string link)
        {
            // Get the file size from the link
            Result<long, DownloadError> fileSize = await HttpNetworkHelper.GetLinkFileSizeAsync(link);
            // If the file size is not successfully retrieved, return result with error message
            if (!fileSize.IsSuccess)
                return fileSize.Map<HttpDownloadContext>(x => null);

            // Otherwise, continue to create the download context
            // Get the target path to save the downloaded file
            string targetPath = GetDownloadSavedPath(link, savedPath);
            // Since GetFileSegments() method requires a file size greater than 0,
            // this if case is used to handle the case where the file size is 0
            if (fileSize.ValueOrNull() == 0)
                return Result<HttpDownloadContext, DownloadError>.Success(
                    new HttpDownloadContext(targetPath, maxParallelThreads, link, GetEmptyRanges(maxParallelThreads)));
            // Otherwise, we can proceed to caculate the file segments for the download threads
            // Get download size for each download thread
            return FileSegmentHelper.CalculateFileSegmentRanges(fileSize.ValueOrNull(), maxParallelThreads)
                .AndThen(fileRange => Result<HttpDownloadContext, DownloadError>.Success(
                    new HttpDownloadContext(targetPath, maxParallelThreads, link, rangePositions: fileRange)));
        }

        /// <summary>
        /// Gets the file path to be used for the download.
        /// </summary>
        /// <param name="link">The URL of the file to be downloaded.</param>
        /// <param name="savedPath">The path where the downloaded file will be saved.</param>
        /// <returns>The file path to be used for the download.</returns>
        private static string GetDownloadSavedPath(string link, string savedPath)
        {
            // Get and return the target path to a unique file name
            return PathHelper.GetUniqueFileName(PathHelper.GetDirectoryNameSafe(savedPath), GetDownloadFileName(link, savedPath));
        }

        /// <summary>
        /// Gets the download file name from the link and saved path.
        /// </summary>
        /// <param name="link">The URL of the file to be downloaded.</param>
        /// <param name="savedPath">The path where the downloaded file will be saved.</param>
        /// <returns>The file name to be used for the download.</returns>
        private static string GetDownloadFileName(string link, string savedPath)
        {
            //FIXED: There is a fix for the issue of empty file name
            // If the file name is not specified, use the file name from the link
            string targetFileName = Path.GetFileName(savedPath);
            if (string.IsNullOrEmpty(targetFileName))
                targetFileName = Path.GetFileName(link);
            // If the file name is also not specified, use a random file name
            if (string.IsNullOrEmpty(targetFileName))
                targetFileName = Path.GetRandomFileName();
            return targetFileName;
        }

        /// <summary>
        /// Creates an empty range for each thread when the file size is 0.
        /// </summary>
        /// <param name="threadCount">The number of threads to create empty ranges for.</param>
        /// <returns>The empty ranges for each thread.</returns>
        private static long[,] GetEmptyRanges(byte threadCount)
        {
            // FIXED: If the file size is 0, we cannot download the file with multiple threads.
            // Here we create an empty range for each thread
            // which means that each thread will not download any data.
            // Create an empty range for each thread
            long[,] emptyRanges = new long[threadCount, 2];
            for (int i = 0; i < threadCount; i++)
            {
                emptyRanges[i, 0] = 0;
                emptyRanges[i, 1] = 0;
            }
            return emptyRanges;
        }
    }
}