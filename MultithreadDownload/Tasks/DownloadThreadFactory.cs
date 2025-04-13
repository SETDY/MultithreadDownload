using MultithreadDownload.Core;
using MultithreadDownload.Threading;

namespace MultithreadDownload.Tasks
{
    public class DownloadThreadFactory : IDownloadThreadFactory
    {
        public IDownloadThread Create(int id, IDownloadContext downloadContext, DownloadWorkDelegate work)
        {
            return new DownloadThread(id, downloadContext, work);
        }
    }
}