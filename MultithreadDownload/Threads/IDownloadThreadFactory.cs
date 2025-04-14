using MultithreadDownload.Core;
using MultithreadDownload.Threads;
using System;

namespace MultithreadDownload.Threading
{
    public interface IDownloadThreadFactory
    {
        IDownloadThread Create(int id, IDownloadContext downloadContext, Action work);
    }
}