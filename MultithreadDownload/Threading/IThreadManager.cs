using MultithreadDownload.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Threading
{
    /// <summary>
    /// Interface for managing download threads.
    /// </summary>
    public interface IThreadManager
    {
        void Start();

        void Pause();

        void Resume();

        void Stop();

        void AddThread(DownloadThread thread);

        IEnumerable<DownloadThread> GetThreads();

        event Action<DownloadThread> ThreadCompleted;
    }
}
