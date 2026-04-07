using MultithreadDownload.Primitives;
using MultithreadDownload.Tasks;
using System;
using System.Collections.Generic;
using System.Text;

namespace MultithreadDownload.Threads.Events
{
    /// <summary>
    /// Represents the event data for download thread events, which contains the context of the download thread.
    /// </summary>
    public class DownloadThreadDataEventArgs
    {
        /// <summary>
        /// The download context of the download thread
        /// </summary>
        public Option<DownloadThreadContext> Context { get; private set; }

        public DownloadThreadDataEventArgs() { }

        public DownloadThreadDataEventArgs(DownloadThreadContext context)
        {
            this.Context = Option<DownloadThreadContext>.Some(context);
        }
    }
}
