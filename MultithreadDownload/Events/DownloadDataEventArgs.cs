using MultithreadDownload.Core;
using MultithreadDownload.Tasks;
using System;

namespace MultithreadDownload.Events
{
    /// <summary>
    /// Represents the event arguments for download data events.
    /// </summary>
    public class DownloadDataEventArgs : EventArgs
    {

        public DownloadTask DownloadTask { get; private set; }

        public DownloadDataEventArgs()
        { }

        public DownloadDataEventArgs(DownloadTask downloadTask)
        {
            this.DownloadTask = downloadTask;
        }
    }
}