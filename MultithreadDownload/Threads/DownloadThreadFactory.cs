using MultithreadDownload.Protocols;
using MultithreadDownload.Threads;
using MultithreadDownload.Primitives;
using System;
using System.IO;
using MultithreadDownload.Core.Errors;

namespace MultithreadDownload.Threading
{
    public class DownloadThreadFactory : IDownloadThreadFactory
    {
        public IDownloadThread Create(int id, IDownloadContext downloadContext, string fileSegmentPath, Func<Stream, Stream, IDownloadThread, Result<bool, DownloadError>> work)
        {
            return new DownloadThread(id, downloadContext, fileSegmentPath, work);
        }
    }
}