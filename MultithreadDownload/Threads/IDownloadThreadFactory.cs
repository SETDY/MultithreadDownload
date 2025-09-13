using MultithreadDownload.Protocols;
using MultithreadDownload.Threads;
using MultithreadDownload.Primitives;
using System;
using System.IO;
using MultithreadDownload.Core.Errors;

namespace MultithreadDownload.Threading
{
    public interface IDownloadThreadFactory
    {
        IDownloadThread Create(byte id, IDownloadContext downloadContext,string fileSegmentPath, Func<Stream, Stream, IDownloadThread, Result<bool, DownloadError>> work);
    }
}