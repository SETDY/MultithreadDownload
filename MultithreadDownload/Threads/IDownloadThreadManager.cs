using MultithreadDownload.Threads;
using MultithreadDownload.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using MultithreadDownload.Core.Errors;
using MultithreadDownload.Logging;

namespace MultithreadDownload.Threading
{
    /// <summary>
    /// Interface for managing download threads.
    /// </summary>
    public interface IDownloadThreadManager : IDisposable
    {
        /// <summary>
        /// The number of completed threads.
        /// </summary>
        byte CompletedThreadsCount { get; }

        /// <summary>
        /// The maximum number of threads that can be used for downloading.
        /// </summary>
        byte MaxParallelThreads { get; }

        /// <summary>
        /// Event that is triggered when a download thread is completed.
        /// </summary>

        event Action<IDownloadThread> ThreadCompleted;

        /// <summary>
        /// Starts the download threads with the given input and output streams.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="outputStream"></param>
        void Start(Stream[] inputStream, Stream[] outputStream);

        /// <summary>
        /// Starts the download threads with the given input and output streams.
        /// </summary>
        void Pause();

        /// <summary>
        /// Resumes the download threads.
        /// </summary>
        void Resume();

        /// <summary>
        /// Cancels the download threads.
        /// </summary>
        void Cancel();

        /// <summary>
        /// Creates new download threads with the maximum number of threads.
        /// </summary>
        /// <returns>Whether the threads was created successfully or not.</returns>
        /// <remarks>
        /// The work delegate is the main download work that will be executed by the download thread.
        /// The main download work is IDownloadSerivce.DownloadFile()
        /// </remarks>
        Result<bool, DownloadError> CreateThreads(Func<Stream, Stream, IDownloadThread, Result<bool, DownloadError>> mainWork, DownloadScopedLogger logger);

        /// <summary>
        /// Creates a new download thread.
        /// </summary>
        /// <returns>Whether the thread was created successfully or not.</returns>
        /// <remarks>
        /// The work delegate is the main download work that will be executed by the download thread.
        /// The main download work is IDownloadSerivce.DownloadFile()
        /// </remarks>
        Result<bool, DownloadError> CreateThread(string fileSegmentPath, Func<Stream, Stream, IDownloadThread, Result<bool, DownloadError>> mainWork, DownloadScopedLogger logger);
        // TODO: Refactor all context structure

        /// <summary>
        /// Gets the list of download threads.
        /// </summary>
        /// <returns>The list of download threads.</returns>
        List<IDownloadThread> GetThreads();
    }
}