using MultithreadDownload.Protocols;
using MultithreadDownload.Threads;
using System;
using System.IO;

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