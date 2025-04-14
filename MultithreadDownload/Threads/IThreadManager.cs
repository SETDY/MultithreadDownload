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
    public interface IDownloadThreadManager
    {
        int CompletedThreadsCount { get; }

        event Action<IDownloadThread> ThreadCompleted;

        void Start(Stream inputStream, Stream[] outputStream);

        void Pause();

        void Resume();

        void Cancel();

        Result<bool> CreateThread(Action mainWork);

        IEnumerable<IDownloadThread> GetThreads();
    }
}