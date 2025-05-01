using MultithreadDownload.Protocols;
using MultithreadDownload.Threads;
using System;
using System.IO;

namespace MultithreadDownload.Threading
{
    public interface IDownloadThreadFactory
    {
        IDownloadThread Create(int id, IDownloadContext downloadContext, Action<Stream, Stream, IDownloadThread> work);
    }
}