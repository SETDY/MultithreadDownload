using MultithreadDownload.Threads;
using MultithreadDownload.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace MultithreadDownload.Threading
{
    /// <summary>
    /// Interface for managing download threads.
    /// </summary>
    public interface IDownloadThreadManager : IDisposable
    {
        byte CompletedThreadsCount { get; }

        byte MaxParallelThreads { get; }

        event Action<IDownloadThread> ThreadCompleted;

        void Start(Stream[] inputStream, Stream[] outputStream);

        void Pause();

        void Resume();

        void Cancel();

        Result<bool> CreateThread(Action<Stream, Stream, IDownloadThread> mainWork);

        IEnumerable<IDownloadThread> GetThreads();
    }
}