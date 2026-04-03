using MultithreadDownload.Protocols.Abstractions;

namespace MultithreadDownload.Threads.Abstractions
{
    /// <summary>
    /// Factory interface for creating instances of ThreadManager.
    /// </summary>
    public interface IDownloadThreadManagerFactory
    {
        public IDownloadThreadManager Create(IDownloadThreadFactory downloadThreadFactory, IDownloadTarget downloadContext, byte maxThreads);
    }
}