using MultithreadDownload.Core;
using MultithreadDownload.Threads;

namespace MultithreadDownload.Threading
{
    public class DownloadThreadFactory : IDownloadThreadFactory
    {
        public IDownloadThread Create(int id, IDownloadContext downloadContext, DownloadWorkDelegate work)
        {
            return new DownloadThread(id, downloadContext, work);
        }
    }
}