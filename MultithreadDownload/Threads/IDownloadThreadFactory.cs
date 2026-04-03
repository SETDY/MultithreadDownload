using MultithreadDownload.Threads;
using MultithreadDownload.Primitives;
using System;
using System.IO;
using MultithreadDownload.Core.Errors;
using MultithreadDownload.Protocols.Abstractions;

namespace MultithreadDownload.Threading
{
    public interface IDownloadThreadFactory
    {
        IDownloadThread Create(byte id, IDownloadTarget downloadContext,string fileSegmentPath, Func<Stream, Stream, IDownloadThread, Result<bool, DownloadError>> work);
    }
}