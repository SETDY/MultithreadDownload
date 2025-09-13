using MultithreadDownload.Core.Errors;
using MultithreadDownload.Primitives;

namespace MultithreadDownload.Protocols
{
    /// <summary>
    /// Interface for download context.
    /// e.g. target path, progress reporter, cancellation token.
    /// </summary>
    public interface IDownloadContext
    {
        /// <summary>
        /// The target path where the downloaded file will be saved.
        /// </summary>
        public string TargetPath { get; }

        /// <summary>
        /// The number of threads required for the download.
        /// </summary>
        public byte ThreadCount { get;}

        /// <summary>
        /// The download range for each download thread.
        /// </summary>
        /// <remarks>
        /// rangePosition[n,0] = startPostion; rangePosition[n,1] = endPosition
        /// </remarks>
        public long[,] RangePositions { get; }

        /// <summary>
        /// The size of the file has been downloaded.
        /// </summary>
        public Result<bool, DownloadError> IsPropertiesVaild();

        /// <summary>
        /// Gets the range size of the file for a thread to downloaded.
        /// </summary>
        /// <returns>The range size of the file in bytes.</returns>
        public long GetRangeSize(byte threadID);
    }
}