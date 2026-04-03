using MultithreadDownload.Primitives;
using System;
using System.IO;
using MultithreadDownload.Core.Errors;
using MultithreadDownload.Protocols.Abstractions;
using MultithreadDownload.Threads.Abstractions;

namespace MultithreadDownload.Threading
{
    public class DownloadThreadFactory : IDownloadThreadFactory
    {
        public IDownloadThread Create(byte id, IDownloadTarget downloadContext, string fileSegmentPath, Func<Stream, Stream, IDownloadThread, Result<bool, DownloadError>> work)
        {
            return new DownloadThread(id, downloadContext, fileSegmentPath, work);
        }
    }
}