using MultithreadDownload.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Tasks
{
    public class DownloadThread : IDownloadThread
    {
        /// <summary>
        /// The ID of the download thread. This is used to identify the thread in the download task.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The download context that contains information about the download operation.
        /// </summary>
        public IDownloadContext DownloadContext { get; private set; }

        /// <summary>
        /// The status of the download thread.
        /// </summary>
        public bool IsAlive
        {
            get
            {
                if (WorkerThread != null)
                {
                    return WorkerThread.IsAlive;
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
        /// The size of the file that has been downloaded by this thread.
        /// </summary>
        public long CompletedBytesSizeCount { get; internal set; }

        /// <summary>
        /// The download progress of the file that has been downloaded by this thread.
        /// </summary>
        public sbyte Progress { get; set; }

        /// <summary>
        /// The cancellation token source for cancelling the download operation.
        /// </summary>
        private CancellationTokenSource Cancellation { get; set; }
        IDownloadContext IDownloadThread.DownloadContext { get => DownloadContext; set => DownloadContext = value; }

        public DownloadThread(int id, IDownloadContext downloadContext, Thread workerThread)
        {
            // Initialize the properties
            this.ID = id;
            this.DownloadContext = downloadContext;
            this.WorkerThread = workerThread;
            this.Cancellation = new CancellationTokenSource();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        internal void AddCompletedBytesSizeCount(long size)
        {
            // Set the completed bytes size count for this thread
            CompletedBytesSizeCount += size;
        }

        /// <summary>
        /// Sets the download progress for this thread.
        /// </summary>
        /// <param name="progress">The progress value to set.</param>
        public void SetDownloadProgress(sbyte progress)
        {
            // If the progress is less than -1 or greater than 100, throw an exception
            if (progress < -1 || progress > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(progress), "Progress must be between 0 and 100.");
            }
            if (progress == -1 || progress == 100)
            {
                // If the progress is -1, it indicates that the download has been cancelled.
                // If the progress is 100, it indicates that the download is complete
                CancellationTokenSource.Cancel();
            }
            // Report the download progress
            ProgressReporter.Report(progress);
        }

        /// <summary>
        /// Cancels the download operation.
        /// </summary>
        /// <returns>Whether the cancellation was successful.</returns>
        public Result<bool> Cancel()
        {
            // If the thread is null or not alive, return failure result.
            // Otherwise, cancel the thread and wait for it to finish
            if (WorkerThread == null) { return Result<bool>.Failure("Thread is not exist so it cannot be cancelled"); }
            if (WorkerThread.IsAlive == false) { return Result<bool>.Failure("Thread is not alive so it cannot be cancelled"); }
            CancellationTokenSource.Cancel();
            WorkerThread.Join();
            return Result<bool>.Success(true);
        }
    }
}
