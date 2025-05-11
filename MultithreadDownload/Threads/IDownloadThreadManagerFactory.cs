using MultithreadDownload.Protocols;

namespace MultithreadDownload.Threading
{
    /// <summary>
    /// Factory interface for creating instances of ThreadManager.
    /// </summary>
    public interface IDownloadThreadManagerFactory
    {
        public IDownloadThreadManager Create(IDownloadThreadFactory downloadThreadFactory, IDownloadContext downloadContext, byte maxThreads);
    }
}