using MultithreadDownload.Core.Errors;
using MultithreadDownload.Logging;
using MultithreadDownload.Primitives;
using MultithreadDownload.Protocols.Abstractions;
using MultithreadDownload.Threads.Core;
using System;
using System.IO;

namespace MultithreadDownload.Threads.Abstractions
{
    public interface IDownloadThreadFactory<TTarget> where TTarget : IDownloadTarget
    {
        public IDownloadThread<TTarget> Create(
            DownloadThreadContext context,
            TTarget target,
            IDownloadService<TTarget> service,
            DownloadScopedLogger logger);
    }
}