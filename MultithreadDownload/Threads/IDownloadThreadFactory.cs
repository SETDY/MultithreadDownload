using MultithreadDownload.Protocols;
using MultithreadDownload.Threads;
using MultithreadDownload.Utils;
using System;
using System.IO;

namespace MultithreadDownload.Threading
{
    public interface IDownloadThreadFactory
    {
        IDownloadThread Create(int id, IDownloadContext downloadContext,string fileSegmentPath, Func<Stream, Stream, IDownloadThread, Result<bool>> work);
    }
}