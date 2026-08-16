using MultithreadDownload.Protocols.Abstractions;
using MultithreadDownload.Threading;
using MultithreadDownload.Threads.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Tasks.Core
{
    public class DownloadTaskContext
    {
        /// <summary>
        /// The Id of the download task, which is a unique identifier for each download task.
        /// </summary>
        public Guid Id { get; }

        public IDownloadTarget DownloadTarget { get; }

        //public DownloadStateMachine StateMachine { get; }

        /// <summary>
        /// The total size of the file has be downloaded, in bytes.
        /// </summary>
        public long TotalDownloaded { get; private set; }

        /// <summary>
        /// The thread manager is uesd for this download task to manage the threads for downloading the file.
        /// </summary>
        public List<DownloadThreadContext> ThreadContexts { get;}

        /// <summary>
        /// The maximum number of parallel threads for downloading the file for this download task.
        /// </summary>
        public byte MaxParallelThreads { get; }

        /// <summary>
        /// The total number of threads for downloading the file for this download task.
        /// </summary>
        public byte TotalThreads => (byte)this.ThreadContexts.Count;

        public DownloadTaskContext(
            Guid id,
            IDownloadTarget target,
            byte maxThreads)
        {
            this.Id = id;
            this.DownloadTarget = target;
            this.MaxParallelThreads = maxThreads;

            this.ThreadContexts = new List<DownloadThreadContext>();
            //this.StateMachine = new DownloadStateMachine();
        }

        /// <summary>
        /// Updates the total downloaded byte count by adding the specified number of bytes.
        /// </summary>
        /// <param name="bytes">The number of bytes to add to the total downloaded count. Must be zero or greater.</param>
        internal void ReportProgress(long bytes)
        {
            this.TotalDownloaded += bytes;
        }
    }
}
