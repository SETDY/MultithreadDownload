using MultithreadDownload.Core;

namespace MultithreadDownload.Threading
{
    /// <summary>
    /// Factory interface for creating instances of DownloadThreadManager.
    /// </summary>
    public interface IDownloadThreadManagerFactory
    {
        public IDownloadThreadManager Create(IDownloadThreadFactory downloadThreadFactory, IDownloadContext downloadContext,
            DownloadWorkDelegate work, byte maxThreads);
    }
}