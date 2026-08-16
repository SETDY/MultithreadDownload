using MultithreadDownload.Core.Errors;
using MultithreadDownload.Logging;
using MultithreadDownload.Primitives;
using MultithreadDownload.Protocols.Abstractions;
using MultithreadDownload.Threads.Abstractions;
using System;
using System.IO;

namespace MultithreadDownload.Threads.Core
{
    public class DownloadThreadFactory<TTarget> : IDownloadThreadFactory where TTarget : IDownloadTarget
    {
        public IDownloadThread<TTarget> Create(
            TTarget target,
            IDownloadService<TTarget> service, 
            DownloadScopedLogger logger)
        {
            return new DownloadThread<TTarget>(target, service, logger);
        }
    }
}