using MultithreadDownload.Core;
using MultithreadDownload.Downloads;
using MultithreadDownload.Threading;
using MultithreadDownload.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Tasks
{
    public class DownloadThread : IDownloadThread
    {
        /// <summary>
        /// The ID of the download thread. This is used to identify the thread in the download task.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// The state of the download thread. This indicates whether the thread is running, paused, or stopped.
        /// </summary>
        public DownloadTaskState State { get; set; }

        /// <summary>
        /// The download context that contains information about the download operation.
        /// </summary>
        public IDownloadContext DownloadContext { get; private set; }

        /// <summary>
        /// The task that will execute the download operation.
        /// </summary>
        public Task WorkerTask { get; set; }

        /// <summary>
        /// The size of the file that has been downloaded by this thread.
        /// </summary>
        public long CompletedBytesSizeCount { get; internal set; }

        /// <summary>
        /// The status of the download thread.
        /// </summary>
        public bool IsAlive { get; private set; }

        /// <summary>
        /// The download progress of the file that has been downloaded by this thread.
        /// </summary>
        public IProgress<sbyte> Progresser { get; private set; }

        /// <summary>
        /// The event that is triggered when the download thread is completed.
        /// </summary>
        public event Action<IDownloadThread> Completed;

        /// <summary>
        /// The cancellation token source for cancelling the download operation.
        /// </summary>
        private readonly CancellationTokenSource s_cancellation;

        private readonly DownloadWorkDelegate s_work;

        public DownloadThread(int id, IDownloadContext downloadContext, DownloadWorkDelegate work)
        {
            // Initialize the properties
            this.ID = id;
            this.DownloadContext = downloadContext;
            this.s_work = work;
            this.s_cancellation = new CancellationTokenSource();
        }

        /// <summary>
        /// Starts the download operation in a new thread.
        /// </summary>
        public void Start()
        {
            this.IsAlive = true;
            this.WorkerTask = Task.Run(() =>
            {
                this.s_work(this);
                this.IsAlive = false;
                this.Completed?.Invoke(this);
            });
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

        public void SetProgresser(IProgress<sbyte> progresser)
        {
            this.Progresser = progresser;
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
                this.s_cancellation.Cancel();
            }
            // Report the download progress
            this.Progresser.Report(progress);
        }

        /// <summary>
        /// Cancels the download operation.
        /// </summary>
        /// <returns>Whether the cancellation was successful.</returns>
        public Result<bool> Cancel()
        {
            // If the thread is null or not alive, return failure result.
            // Otherwise, cancel the thread and wait for it to finish
            if (this.s_work == null) { return Result<bool>.Failure("Thread is not exist so it cannot be cancelled"); }
            if (this.IsAlive == false) { return Result<bool>.Failure("Thread is not alive so it cannot be cancelled"); }
            this.s_cancellation.Cancel();
            return Result<bool>.Success(true);
        }
    }
}