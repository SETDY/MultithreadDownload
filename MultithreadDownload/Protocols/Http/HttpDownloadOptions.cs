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
        public uint BufferSize { get; init; } = 4096;

        public uint MaxRetries { get; init; } = 5;

        public uint BaseRetryDelayMs { get; init; } = 2000;

        /// <summary>
        /// The maximum retry delay in milliseconds, which is calculated based on the base retry delay and the maximum number of retries using an exponential backoff strategy. <br/>
        /// </summary>
        public uint MaxRetryDelayMs
        {
            get
            {
                return (uint)(BaseRetryDelayMs * Math.Pow(2, this.MaxRetries));
            }
        }
    }
}
