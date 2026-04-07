using System;
using System.Collections.Generic;
using System.Text;

namespace MultithreadDownload.Protocols.Http
{
    /// <summary>
    /// The class represents a set of options for configuring the HTTP download process
    /// </summary>
    public sealed class HttpDownloadOptions
    {
        public int BufferSize { get; init; } = 4096;

        public int MaxRetries { get; init; } = 5;

        public int RetryDelayMs { get; init; } = 2000;
    }
}
