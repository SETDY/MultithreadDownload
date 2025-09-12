using MultithreadDownload.Downloads;
using MultithreadDownload.Protocols;
using MultithreadDownload.Threads;
using MultithreadDownload.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MultithreadDownload.Core.Errors;
using MultithreadDownload.Logging;

namespace MultithreadDownload.Threading
{
    public class DownloadThreadManager : IDownloadThreadManager
    {

        /// <summary>
        /// The number of completed threads.
        /// </summary>
        public byte CompletedThreadsCount
        {
            get
            {
                // FIXED: The non-implment problem.
                // Select all the threads that their states are TaskCompleted or Failed or Cancelled, meaning that they has completed the download process.
                // Caculate the number of selected threads which is the completed threads count and return it.
                return (byte)GetThreads()
                    .Where(thread => thread.State is DownloadState.Completed
                    || thread.State is DownloadState.Failed
                    || thread.State is DownloadState.Cancelled)
                    .ToArray()
                    .Length;
            }
        }

        /// <summary>
        /// The number of completed threads.
        /// </summary>
        public byte MaxParallelThreads
        {
            get
            {
                return this._maxThreads;
            }
        }

        /// <summary>
        /// Event that is triggered when a download thread is completed.
        /// </summary>
        public event Action<IDownloadThread> ThreadCompleted;

        /// <summary>
        /// A reference to the list of download threads.
        /// </summary>
        private readonly List<IDownloadThread> _threads;

        /// <summary>
        /// The download context that contains information about the download operation.
        /// </summary>
        private readonly IDownloadContext _downloadContext;

        /// <summary>
        /// The maximum number of threads that can be used for downloading.
        /// </summary>
        private readonly byte _maxThreads;

        /// <summary>
        /// The factory for creating download threads.
        /// </summary>
        private readonly IDownloadThreadFactory _factory;

        /// <summary>
        /// Constructor for the ThreadManager class.
        /// </summary>
        /// <param name="factory">The factory for creating download threads.</param>
        /// <param name="downloadContext">The context that contains information about the download operation.</param>
        /// <param name="maxThreads">The maximum number of threads that can be used for downloading.</param>
        public DownloadThreadManager(IDownloadThreadFactory factory, byte maxThreads, IDownloadContext downloadContext)
        {
            // Initialize the properties
            _threads = new List<IDownloadThread>(maxThreads);
            _downloadContext = downloadContext;
            _maxThreads = maxThreads;
            _factory = factory;
        }

        /// <summary>
        /// Creates a new download thread with the maximum number of threads and the given download context.
        /// </summary>
        /// <param name="mainWork">The method which the task will excecute to download the file.</param>
        /// <param name="logger">The logger to log the infomation about this DownloadThreadManagement.</param>
        /// <returns>Whether the thread was created successfully or not.</returns>
        /// <remarks>
        /// The work delegate is the main download work that will be executed by the download thread.
        /// The main download work is IDownloadSerivce.DownloadFile()
        /// </remarks>
        public Result<bool, DownloadError> CreateThreads(Func<Stream, Stream, IDownloadThread, Result<bool, DownloadError>> mainWork, DownloadScopedLogger logger)
        {
            // Check if the target file already exists
            // If it does, return a failure result
            // Otherwise, split the file paths and create new download threads with the maximum number of threads
            if (File.Exists(_downloadContext.TargetPath))
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.FileAlreadyExists, "The final file already exists."));
            if (_threads.Count != 0)
                throw new InvalidOperationException("The download threads already exist. Please dispose of them before creating new ones.");
            // return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.ThreadCreationFailed, "The download threads already exist. Please dispose of them before creating new ones."));

            // Split the file paths into segments based on the maximum number of threads allowed
            // Then, create new download threads with the main work delegate
            // Log that the threads are being created 
            logger.LogInfo($"Strart the process of creating {MaxParallelThreads} download threads for file segments in {_downloadContext.TargetPath}.");
            return FileSegmentHelper
                .SplitPaths(MaxParallelThreads, _downloadContext.TargetPath)
                .AndThen(segmentPaths => CreateThreads(MaxParallelThreads, segmentPaths, mainWork, logger));
        }

        /// <summary>
        /// Creates new download threads.
        /// </summary>
        /// <returns>Whether the threads was created successfully or not.</returns>
        /// <param name="maxParallelThreads">The maximum number of the parallel-running threads.</param>
        /// <param name="fileSegmentPaths">The paths for each of the file segment to be saved.</param>
        /// <param name="logger">The logger to log the infomation about this DownloadThreadManagement.</param>
        /// <remarks>
        /// The work delegate is the main download work that will be executed by the download thread.
        /// The main download work is IDownloadSerivce.DownloadFile()
        /// </remarks>
        private Result<bool, DownloadError> CreateThreads(byte maxParallelThreads, string[] fileSegmentPaths, Func<Stream, Stream, IDownloadThread, Result<bool, DownloadError>> mainWork, DownloadScopedLogger logger)
        {
            // Creates new download threads with the given number of threads.
            // Check whether the number of threads is greater than 0 and equal to the number of file segments.
            // TODO: Partial Create Threads
            if (maxParallelThreads <= 0 || maxParallelThreads != fileSegmentPaths.Length)
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.ArgumentOutOfRange, "The number of threads must be greater than 0 and equal to the number of file segments."));

            logger.LogInfo($"Succese to get the file segment paths. Continue to create the threads.");
            logger.LogInfo($"Try to Create {maxParallelThreads} download threads for file segments in {_downloadContext.TargetPath}.");
            IEnumerable<Result<bool, DownloadError>> resultEnumertor = fileSegmentPaths
                .Select(fileSegmentPath => CreateThread(fileSegmentPath, mainWork, logger));
            return Result<bool, DownloadError>.AllSucceeded(resultEnumertor);
        }

        /// <summary>
        /// Creates a new download thread.
        /// </summary>
        /// <returns>Whether the thread was created successfully or not.</returns>
        /// <param name="fileSegmentPath">The path for the file segment to be saved.</param>
        /// <param name="mainWork">The method which the task will excecute to download the file.</param>
        /// <param name="logger">The logger to log the infomation about the thread which will be created.</param>
        /// <remarks>
        /// The work delegate is the main download work that will be executed by the download thread.
        /// The main download work is IDownloadSerivce.DownloadFile()
        /// </remarks>
        public Result<bool, DownloadError> CreateThread(string fileSegmentPath, Func<Stream, Stream, IDownloadThread, Result<bool, DownloadError>> mainWork, DownloadScopedLogger logger)
        {
            // Check if the number of threads is greater than the maximum number of threads allowed
            // If it is, return a failure result
            if (this._threads.Count >= this._maxThreads)
                Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.ThreadMaxExceeded, "The number of download threads is at the maximum postition."));
            // Validate the input parameters
            //  - The file segment path cannot be null or empty
            //  - The main work delegate cannot be null
            if (string.IsNullOrEmpty(fileSegmentPath))
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.PathNotFound, "The file segment path cannot be null or empty."));
            if (mainWork is null)
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.NullReference, "The main work delegate cannot be null."));

            // Firstly, Create a new thread with the factory
            // Then, Set the progresser and the logger for the thread
            // Finally, Add the thread to the list of threads
            IDownloadThread downloadThread = 
                this._factory.Create(_threads.Count, _downloadContext, fileSegmentPath, mainWork);
            this.SetThreadProgresser(downloadThread);
            downloadThread.SetLogger(this.GetThreadLogger(logger, downloadThread));
            _threads.Add(downloadThread);
            // Log that the thread is created successfully
            logger.LogInfo($"Download thread {downloadThread.ID} is created successfully for file segment {fileSegmentPath}.");
            return Result<bool, DownloadError>.Success(true);
        }

        /// Get the download logger for the download thread
        private DownloadScopedLogger GetThreadLogger(DownloadScopedLogger threadManagementLogger, IDownloadThread targetThread)
        {
            // Fristly, get the task id from the thread management logger
            // Then, return a new logger that wraps the current logger with the scoped logger
            return DownloadLogger.For(Option<string>.Some(threadManagementLogger.GetContext().Item1), Option<byte>.Some((byte)targetThread.ID));
        }

        /// <summary>
        /// Set the progresser for the thread.
        /// </summary>
        /// <param name="thread">A download thread.</param>
        private void SetThreadProgresser(IDownloadThread thread)
        {
            // Create a thread with new progresser to handle the progress of the thread
            // If the progress is 100, invoke the ThreadCompleted event
            Progress<sbyte> progresser = new Progress<sbyte>(progress =>
            {
                // If the progress is 100 and the thread is downloading,
                // set the state to completed to invoke the event and increment the completed threads count
                if (progress == 100 && thread.State == DownloadState.Downloading)
                {
                    // Log the times that this method is called
                    // Set the state of the thread to completed
                    thread.SetState(DownloadState.Completed);
                    // This if statement checks if the number of completed threads is greater than the maximum number of threads
                    // If it happens, the task may stack and cause a deadlock because the task is waiting for the thread to complete
                    // To prevent that from happening, it throws an exception to break the deadlock
                    if (CompletedThreadsCount > MaxParallelThreads)
                        Cancel();
                    // Check whether the thread is completed or not before incrementing the completed threads count
                    // To ensure that the thread does not increase the _completedThreadsCount multiple times
                    // Increment the completed threads count by using Interlocked to ensure thread safety
                    ThreadCompleted?.Invoke(thread);
                }
                // If the progress is -1, it indicates that the download has been cancelled or failed.
                else if (progress == -1)
                {
                    // If the progress is -1, it indicates that the download has been cancelled or failed.
                    // In this case, since setting the state of the thread to cancelled or failed has been done in the thread's main work,
                    // we just cancel all the threads and invoke the ThreadCompleted event to notify that the thread has completed its work. 
                    DownloadLogger.LogError($"The thread manager has been notified that the thread {thread.ID} has been cancelled or failed.");
                    Cancel();
                    ThreadCompleted?.Invoke(thread);
                }
            });
            thread.SetProgresser(progresser);
        }

        /// <summary>
        /// Starts all download threads that has been created.
        /// </summary>
        /// <param name="inputStreams">The input streams to read from.</param>
        /// <param name="outputStreams">The output streams of each of threads to write to.</param>
        public void Start(Stream[] inputStreams, Stream[] outputStreams)
        {
            // Validate the input parameters
            // If the input streams or output streams are null, throw an exception
            if (inputStreams == null || outputStreams == null)
                throw new NullReferenceException($"Cannot start download threads with null input or output streams.");
            // Since a input stream and a output stream is a pair, the number of input streams must be equal to the number of output streams
            // If the number of input streams is not equal to the number of output streams, throw an exception
            if (inputStreams.Length != outputStreams.Length)
                throw new ArgumentException("The number of input streams must be equal to the number of output streams.");
            // If the number of input streams or output streams is less than or equal to 0, throw an exception
            if (inputStreams.Length == 0 || outputStreams.Length == 0)
                throw new ArgumentException("The number of input streams and output streams must be greater than 0.");
            // If the length of the output and input streams is not equal to the number of threads, throw an exception
            if (outputStreams.Length != _threads.Count || inputStreams.Length != _threads.Count)
                    throw new ArgumentException("The number of output streams and input streams must be equal to the number of threads.");
            // Otherwise, start each thread with the input stream and the corresponding output stream
            for (int i = 0; i < outputStreams.Length; i++)
            {
                _threads[i].Start(inputStreams[i], outputStreams[i]);
            }
        }

        /// <summary>
        /// Pauses all download threads.
        /// </summary>
        public void Pause()
        {
            foreach (IDownloadThread thread in _threads)
            {
                thread.Pause();
            }
        }

        /// <summary>
        /// Resumes all download threads.
        /// </summary>
        public void Resume()
        {
            foreach (IDownloadThread thread in _threads)
            {
                thread.Resume();
            }
        }

        /// <summary>
        /// Stops all download threads.
        /// </summary>
        public void Cancel()
        {
            foreach (IDownloadThread thread in _threads)
            {
                thread.Cancel();
            }
        }

        public void Dispose()
        {
            // Dispose of each thread in the list
            foreach (IDownloadThread thread in _threads)
            {
                thread.Dispose();
            }
            _threads.Clear();
        }

        /// <summary>
        /// Gets the list of download threads.
        /// </summary>
        /// <returns>The list of download threads.</returns>
        public List<IDownloadThread> GetThreads() => _threads;
    }
}