using MultithreadDownload.Core;
using MultithreadDownload.Help;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Threading
{
    public class DownloadThread
    {
        /// <summary>
        /// The ID of the download thread. This is used to identify the thread in the download task.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The download context that contains information about the download operation.
        /// </summary>
        public readonly IDownloadContext DownloadContext;

        /// <summary>
        /// The status of the download thread.
        /// </summary>
        public bool IsAlive
        {
            get
            {
                if (this.WorkerThread != null)
                {
                    return this.WorkerThread.IsAlive;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// The thread that will execute the download operation.
        /// </summary>
        public Thread WorkerThread { get; set; }

        /// <summary>
        /// The progress reporter for reporting download progress.
        /// </summary>
        public IProgress<long> ProgressReporter { get; set; }

        /// <summary>
        /// The cancellation token for cancelling the download operation.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        public DownloadThread(int iD, IDownloadContext downloadContext, Thread workerThread, IProgress<long> progressReporter, CancellationToken cancellationToken)
        {
            // Initialize the properties
            ID = iD;
            DownloadContext = downloadContext;
            WorkerThread = workerThread;
            ProgressReporter = progressReporter;
            CancellationToken = cancellationToken;
        }
    }
}
