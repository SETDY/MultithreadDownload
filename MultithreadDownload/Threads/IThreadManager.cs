using MultithreadDownload.Threads;
using MultithreadDownload.Utils;
using System;
using System.Collections.Generic;

namespace MultithreadDownload.Threading
{
    /// <summary>
    /// Interface for managing download threads.
    /// </summary>
    public interface IDownloadThreadManager
    {
        int CompletedThreadsCount { get; }

        event Action<IDownloadThread> ThreadCompleted;

        void Start();

        void Pause();

        void Resume();

        void Cancel();

        Result<bool> CreateThread();

        IEnumerable<IDownloadThread> GetThreads();
    }
}