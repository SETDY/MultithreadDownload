using MultithreadDownload.Threads;
using System.IO;
using System;
using MultithreadDownload.Protocols;

namespace MultithreadDownload.Threading
{
    public class DownloadThreadFactory : IDownloadThreadFactory
    {
        public IDownloadThread Create(int id, IDownloadContext downloadContext, Action<Stream, Stream, IDownloadThread> work)
        {
            return new DownloadThread(id, downloadContext, work);
        }
    }
}