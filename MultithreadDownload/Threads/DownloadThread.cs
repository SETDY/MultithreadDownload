using MultithreadDownload.Downloads;
using MultithreadDownload.Logging;
using MultithreadDownload.Protocols;
using MultithreadDownload.Threads;
using MultithreadDownload.Primitives;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MultithreadDownload.Core.Errors;

namespace MultithreadDownload.Threading
{
    public sealed class DownloadThread : IDownloadThread, IDisposable
    {
        /// <summary>
        /// The ID of the download thread. This is used to identify the thread in the download task.
        /// </summary>
        public byte ID { get; private set; }

        /// <summary>
        /// The state of the download thread. This indicates whether the thread is running, paused, or stopped.
        /// </summary>
        public DownloadState State { get; internal set; }

        /// <summary>
        /// The download context that contains information about the download operation.
        /// </summary>
        public IDownloadContext DownloadContext { get; private set; }

        /// <summary>
        /// The path to the file segment that this thread is responsible for downloading.
        /// </summary>
        public string FileSegmentPath { get; internal set; }

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
        /// The logger instance for logging messages related to the download thread.
        /// </summary>
        public DownloadScopedLogger Logger { get; private set; }

        /// <summary>
        /// The cancellation token source for cancelling the download operation.
        /// </summary>
        private readonly CancellationTokenSource _cancellation;

        /// <summary>
        /// The function that will be executed in the download thread to perform the download operation.
        /// </summary>
        /// <remarks>
        /// For example, <see cref="Protocols.Http.HttpDownloadService"/>.DownloadFile(Stream inputStream, Stream outputStream, IDownloadThread downloadThread) 
        /// is a work of download threads.
        /// </remarks>
        private readonly Func<Stream, Stream, IDownloadThread, Result<bool, DownloadError>> _work;

        public DownloadThread(byte id, IDownloadContext downloadContext, string fileSegmentPath, Func<Stream, Stream, IDownloadThread, Result<bool, DownloadError>> work)
        {
            // Initialize the properties
            this.ID = id;
            this.DownloadContext = downloadContext;
            this.FileSegmentPath = fileSegmentPath;
            this._work = work;
            this._cancellation = new CancellationTokenSource();
        }

        /// <summary>
        /// Starts the download operation in a new thread.
        /// </summary>
        public void Start(Stream inputStream, Stream outputStream)
        {
            this.IsAlive = true;
            this.WorkerTask = Task.Run(() =>
            {
                // Execute the download operation using the provided work function
                Result<bool, DownloadError> downloadResult = _work(inputStream, outputStream, this);
                // If the download operation is failed, log the error and set the state to failed
                // FIXED: This is used to fix when the download operation is failed, the thread will not be completed and the state will not be set to failed.
                downloadResult.OnFailure(error =>
                {
                    // Log the error if the download operation fails
                    DownloadLogger.LogError($"Download thread {ID} failed with error: {error.Message}");
                    // Set the state to failed
                    this.State = DownloadState.Failed;
                    // Report the progress as -1 to indicate failure
                    SetDownloadProgress(-1);
                });
                // Set the thread's properties after the download operation is completed
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

        public void Dispose()
        {
            // Cancel the download operation and clean up resources
            this.Cancel();
        }

        /// <summary>
        /// Sets the logger instance to be used for download-related operations.
        /// </summary>
        /// <param name="logger">The logger to associate with download operations. Cannot be null.</param>
        public void SetLogger(DownloadScopedLogger logger)
        {
            this.Logger = logger;
        }

        // TODO: This method should not be public in term of constructional design, but it is needed for download working perfectly now.
        //       It should be refactored later to be private or internal.
        /// <summary>
        /// Adds the completed bytes size count for this thread.
        /// </summary>
        /// <param name="size">The size of the completed bytes to add.</param>
        /// <exception cref="ArgumentOutOfRangeException">The size is negative.</exception>
        public void AddCompletedBytesSizeCount(long size)
        {
            if (size < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Size cannot be negative.");
            }
            // Set the completed bytes size count for this thread
            this.CompletedBytesSizeCount += size;
            // Log the completed bytes size count
            // DownloadLogger.LogInfo($"Thread ID: {ID}, TaskCompleted Bytes Size Count: {CompletedBytesSizeCount} and add {size} in this round");
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
                if (!this._cancellation.IsCancellationRequested)
                    this._cancellation?.Cancel();
            }
            // Report the download progress
            this.Progresser.Report(progress);
        }

        /// <summary>
        /// Sets the state of the download thread.
        /// </summary>
        /// <param name="taskState">The state of task to set.</param>
        public void SetState(DownloadState taskState)
        {
            // Set the state of the download thread
            this.State = taskState;
        }

        /// <summary>
        /// Cancels the download operation.
        /// </summary>
        /// <returns>Whether the cancellation was successful.</returns>
        public Result<bool, DownloadError> Cancel()
        {
            // If the thread is null or not alive, return failure result.
            // Otherwise, cancel the thread and wait for it to finish
            if (this._work == null)
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.NullReference, "Thread is not exist so it cannot be cancelled"));
            if (this._cancellation == null)
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.NullReference, "Thread cancellation token source is not exist so it cannot be cancelled"));
            if (this.IsAlive == false)
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.ThreadNotFound, "Thread is not alive so it cannot be cancelled"));
            if (this._cancellation.IsCancellationRequested)
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.ThreadCancelled, "Thread is already cancelled"));
            this._cancellation.Cancel();
            this._cancellation.Dispose();
            this.Dispose();
            return Result<bool, DownloadError>.Success(true);
        }
    }
}