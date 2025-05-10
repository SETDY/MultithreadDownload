using MultithreadDownload.CoreTypes.Failures;
using MultithreadDownload.Utils;
using System.IO;
using System.Threading.Tasks;

namespace MultithreadDownload.Protocols
{
    /// <summary>
    /// Represents the context for an HTTP download.
    /// </summary>
    public class HttpDownloadContext : IDownloadContext
    {
        #region Properties
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
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpDownloadContext"/> class.
        /// </summary>
        /// <param name="targetPath">The target path where the downloaded file will be saved.</param>
        /// <param name="url">The URL of the file to be downloaded.</param>
        /// <param name="rangePositions">The range positions for each download thread.</param>
        private HttpDownloadContext(string targetPath, string url, long[,] rangePositions)
        {
            // Initialize the properties
            this.TargetPath = targetPath;
            this.RangePositions = rangePositions;
            this.Url = url;
            this.CompletedSize = 0;
        }

        /// <summary>
        /// Checks if the properties of the HttpDownloadContext are valid.
        /// </summary>
        /// <returns>Whether the properties are valid or not.</returns>
        public bool IsPropertiesVaild()
        {
            // Check if the target path is valid
            if (string.IsNullOrEmpty(TargetPath) || string.IsNullOrEmpty(Url) || 
                !HttpNetworkHelper.LinkCanConnectionAsync(this.Url).Result || RangePositions == null)
            {
                // Because the target path cannot be null or empty, 
                // the URL cannot be null or empty and it has to been connected,
                // the range start and offset cannot be null.
                return false;
            }
            return true;
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
        public static async Task<Result<HttpDownloadContext, DownloadFailure>> GetDownloadContext(byte maxParallelThreads, string targetPath, string link)
        {
            // Get the final file path by using the PathHelper class to prevent the repetition of file names
            string finalFilePath = PathHelper.GetUniqueFileName(PathHelper.GetDirectoryNameSafe(targetPath), Path.GetFileName(targetPath));

            Result<long[,], DownloadFailure> fileSegmentRanges = (await HttpNetworkHelper.GetLinkFileSizeAsync(link)).AndThen(fileSize =>
            {
                return FileSegmentHelper.CalculateFileSegmentRanges(fileSize, maxParallelThreads);
            });

            if (fileSegmentRanges.IsSuccess)
                // Get download size for each download thread
                return Result<HttpDownloadContext, DownloadFailure>.Success(
                    new HttpDownloadContext(targetPath, link, fileSegmentRanges.SuccessValue.Value));

            // Return a failure.
            switch (fileSegmentRanges.FailureReason.Value.Kind)
            {
                case DownloadFailureReason.InvalidUrl:
                    return Result<HttpDownloadContext, DownloadFailure>.Failure(new DownloadFailure(DownloadFailureReason.InvalidUrl, "The link is not a valid for connecting."));
                case DownloadFailureReason.CannotGetFileSize:
                    return Result<HttpDownloadContext, DownloadFailure>.Failure(new DownloadFailure(DownloadFailureReason.CannotGetFileSize, "Cannot get the file size."));
                default:
                    return Result<HttpDownloadContext, DownloadFailure>.Failure(new DownloadFailure(DownloadFailureReason.UnexpectedFailure, "An unexpected error occurred."));
            }
        }
    }
}