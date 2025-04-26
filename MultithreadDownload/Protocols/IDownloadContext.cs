using MultithreadDownload.Utils;

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
        public string TargetPath { get;}

        /// <summary>
        /// The download range for each download thread.
        /// </summary>
        /// <remarks>
        /// rangePosition[n][0] = startPostion; rangePosition[n][1] = endPosition
        /// </remarks>
        public long[][] RangePositions { get; }

        public Result<bool> IsPropertiesVaild();
    }
}