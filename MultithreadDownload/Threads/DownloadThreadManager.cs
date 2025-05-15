using MultithreadDownload.Protocols;
using MultithreadDownload.Threads;
using MultithreadDownload.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace MultithreadDownload.Threading
{
    public class DownloadThreadManager : IDownloadThreadManager
    {
        /// <summary>
        /// Backing field for the CompletedThreadsCount property.
        /// </summary>
        private int _completedThreadsCount;

        /// <summary>
        /// The number of completed threads.
        /// </summary>
        public byte CompletedThreadsCount
        {
            get => (byte)_completedThreadsCount;
            private set => _completedThreadsCount = value;
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
            _threads = new List<IDownloadThread>();
            _downloadContext = downloadContext;
            _maxThreads = maxThreads;
            _factory = factory;
            this.CompletedThreadsCount = 0;
        }

        /// <summary>
        /// Creates a new download thread with the maximum number of threads and the given file segment paths.
        /// </summary>
        /// <returns>Whether the thread was created successfully or not.</returns>
        /// <remarks>
        /// The work delegate is the main download work that will be executed by the download thread.
        /// The main download work is IDownloadSerivce.DownloadFile()
        /// </remarks>
        public Result<bool> CreateThreads(Func<Stream, Stream, IDownloadThread, Result<bool>> mainWork)
        {
            // Check if the target file already exists
            // If it does, return a failure result
            // Otherwise, split the file paths and create new download threads with the maximum number of threads
            if (File.Exists(_downloadContext.TargetPath))
                return Result<bool>.Failure("The final file already exists.");

            Result<string[]> segmentPaths = FileSegmentHelper.SplitPaths(MaxParallelThreads, _downloadContext.TargetPath);
            if (!segmentPaths.IsSuccess)
                return Result<bool>.Failure("SplitPaths failed");
            return CreateThreads(MaxParallelThreads, segmentPaths.Value, mainWork);
        }

        /// <summary>
        /// Creates new download threads.
        /// </summary>
        /// <returns>Whether the threads was created successfully or not.</returns>
        /// <remarks>
        /// The work delegate is the main download work that will be executed by the download thread.
        /// The main download work is IDownloadSerivce.DownloadFile()
        /// </remarks>
        private Result<bool> CreateThreads(byte threadsCount, string[] fileSegmentPaths, Func<Stream, Stream, IDownloadThread, Result<bool>> mainWork)
        {
            // Creates new download threads with the given number of threads.
            if (threadsCount <= 0 || threadsCount != fileSegmentPaths.Length)
                return Result<bool>.Failure("The number of threads must be greater than 0 and equal to the number of file segments.");
            Result<bool> result = Result<bool>.Success(true);
            try
            {
                for (int i = 0; i < threadsCount; i++)
                {
                    result = CreateThread(fileSegmentPaths[i], mainWork);
                }
            }
            catch (Exception)
            {
                result = Result<bool>.Failure("Failed to create thread.");
            }
            return result;
        }

        /// <summary>
        /// Creates a new download thread.
        /// </summary>
        /// <returns>Whether the thread was created successfully or not.</returns>
        /// <remarks>
        /// The work delegate is the main download work that will be executed by the download thread.
        /// The main download work is IDownloadSerivce.DownloadFile()
        /// </remarks>
        public Result<bool> CreateThread(string fileSegmentPath, Func<Stream, Stream, IDownloadThread, Result<bool>> mainWork)
        {
            if (this._threads.Count > this._maxThreads) { Result<bool>.Failure("The number of download threads is at the maximum postition."); }
            // Create a new thread with the factory
            // Set the progresser for the thread
            // Add the thread to the list of threads
            IDownloadThread downloadThread = _factory.Create(0, _downloadContext, fileSegmentPath, mainWork);
            this.SetThreadProgresser(downloadThread);
            _threads.Add(downloadThread);
            return Result<bool>.Success(true);
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
                if (progress == 100)
                {
                    // This if statement checks if the number of completed threads is greater than the maximum number of threads
                    // If it happens, the task may stack and cause a deadlock because the task is waiting for the thread to complete
                    // To prevent that from happening, it throws an exception to break the deadlock
                    if (_completedThreadsCount >= MaxParallelThreads)
                        throw new InvalidDataException("The number of completed threads is greater than the maximum number of threads.");
                    // Increment the completed threads count by using Interlocked to ensure thread safety
                    Interlocked.Increment(ref _completedThreadsCount);
                    ThreadCompleted?.Invoke(thread);
                }
            });
            thread.SetProgresser(progresser);
        }

        /// <summary>
        /// Starts all download threads.
        /// </summary>
        /// <param name="inputStream">The input stream to read from.</param>
        /// <param name="outputStreams">The output streams of each of threads to write to.</param>
        public void Start(Stream[] inputStream, Stream[] outputStreams)
        {
            // If the length of the output streams is not equal to the number of threads, throw an exception
            // Otherwise, start each thread with the input stream and the corresponding output stream
            if (outputStreams.Length != this._threads.Count) { throw new ArgumentException("The number of output streams must be equal to the number of threads."); }
            for (int i = 0; i < outputStreams.Length; i++)
            {
                this._threads[i].Start(inputStream[i], outputStreams[i]);
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