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
    public class DownloadThread : IDownloadThread, IDisposable
    {
        /// <summary>
        /// The ID of the download thread. This is used to identify the thread in the download task.
        /// </summary>
        public int ID { get; private set; }

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

        public DownloadThread(int id, IDownloadContext downloadContext, string fileSegmentPath, Func<Stream, Stream, IDownloadThread, Result<bool, DownloadError>> work)
        {
            // Initialize the properties
            ID = id;
            DownloadContext = downloadContext;
            FileSegmentPath = fileSegmentPath;
            _work = work;
            _cancellation = new CancellationTokenSource();
        }

        /// <summary>
        /// Starts the download operation in a new thread.
        /// </summary>
        public void Start(Stream inputStream, Stream outputStream)
        {
            IsAlive = true;
            WorkerTask = Task.Run(() =>
            {
                _work(inputStream, outputStream, this);
                IsAlive = false;
                Completed?.Invoke(this);
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
            // Cancel the download operation => _task will be cancelled
            _cancellation.Cancel();
            // Dispose of the cancellation token source
            _cancellation.Dispose();
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
            CompletedBytesSizeCount += size;
            // Log the completed bytes size count
            // DownloadLogger.LogInfo($"Thread ID: {ID}, Completed Bytes Size Count: {CompletedBytesSizeCount} and add {size} in this round");
        }

        public void SetProgresser(IProgress<sbyte> progresser)
        {
            Progresser = progresser;
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
                _cancellation.Cancel();
            }
            // Report the download progress
            Progresser.Report(progress);
        }

        /// <summary>
        /// Sets the state of the download thread.
        /// </summary>
        /// <param name="taskState">The state of task to set.</param>
        public void SetState(DownloadState taskState)
        {
            // Set the state of the download thread
            State = taskState;
        }

        /// <summary>
        /// Cancels the download operation.
        /// </summary>
        /// <returns>Whether the cancellation was successful.</returns>
        public Result<bool> Cancel()
        {
            // If the thread is null or not alive, return failure result.
            // Otherwise, cancel the thread and wait for it to finish
            if (_work == null) { return Result<bool>.Failure("Thread is not exist so it cannot be cancelled"); }
            if (IsAlive == false) { return Result<bool>.Failure("Thread is not alive so it cannot be cancelled"); }
            this.Dispose();
            _cancellation.Cancel();
            return Result<bool>.Success(true);
        }
    }
}