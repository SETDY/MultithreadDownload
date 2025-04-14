using MultithreadDownload.Core;
using MultithreadDownload.Threads;

namespace MultithreadDownload.Threading
{
    public interface IDownloadThreadFactory
    {
        IDownloadThread Create(int id, IDownloadContext downloadContext, DownloadWorkDelegate work);
    }
}