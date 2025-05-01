using MultithreadDownload.Protocols;

namespace MultithreadDownload.Threading
{
    /// <summary>
    /// Factory class for creating instances of DownloadThreadManager.
    /// </summary>
    public class DownloadThreadManagerFactory : IDownloadThreadManagerFactory
    {
        /// <summary>
        /// Creates a new instance of the DownloadThreadManager.
        /// </summary>
        /// <param name="downloadThreadFactory">The factory for creating download threads.</param>
        /// <param name="downloadContext">The download context of the download task.</param>
        /// <param name="maxThreads">The maximum number of threads that can be used for downloading.</param>
        /// <returns></returns>
        public IDownloadThreadManager Create(IDownloadThreadFactory downloadThreadFactory, IDownloadContext downloadContext, byte maxThreads)
        {
            return new DownloadThreadManager(downloadThreadFactory, maxThreads, downloadContext);
        }
    }
}