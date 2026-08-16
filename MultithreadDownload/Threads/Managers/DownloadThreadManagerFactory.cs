using MultithreadDownload.Protocols.Abstractions;
using MultithreadDownload.Threading;
using MultithreadDownload.Threads.Abstractions;

namespace MultithreadDownload.Threads.Managers
{
    /// <summary>
    /// Factory class for creating instances of ThreadManager.
    /// </summary>
    public class DownloadThreadManagerFactory : IDownloadThreadManagerFactory
    {
        /// <summary>
        /// Creates a new instance of the ThreadManager.
        /// </summary>
        /// <param name="downloadThreadFactory">The factory for creating download threads.</param>
        /// <param name="downloadContext">The download context of the download task.</param>
        /// <param name="maxThreads">The maximum number of threads that can be used for downloading.</param>
        /// <returns></returns>
        public IDownloadThreadManager Create(IDownloadThreadFactory downloadThreadFactory, IDownloadTarget downloadContext, byte maxThreads)
        {
            return new DownloadThreadManager1(downloadThreadFactory, maxThreads, downloadContext);
        }
    }
}