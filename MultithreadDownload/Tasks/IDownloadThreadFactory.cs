using MultithreadDownload.Core;
using MultithreadDownload.Threading;

namespace MultithreadDownload.Tasks
{
    public interface IDownloadThreadFactory
    {
        IDownloadThread Create(int id, IDownloadContext downloadContext, DownloadWorkDelegate work);
    }
}