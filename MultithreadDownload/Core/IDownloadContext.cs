using MultithreadDownload.Utils;

namespace MultithreadDownload.Core
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
        public string TargetPath { get; set; }

        public Result<bool> IsPropertiesVaild();
    }
}