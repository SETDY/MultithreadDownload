using MultithreadDownload.Core;
using MultithreadDownload.Tasks;
using MultithreadDownload.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultithreadDownload.Threading
{
    public delegate void DownloadWorkDelegate(IDownloadThread thread);

    public class DownloadThreadManager : IDownloadThreadManager
    {
        /// <summary>
        /// The number of completed threads.
        /// </summary>
        public int CompletedThreadsCount { get; private set; }

        /// <summary>
        /// Event that is triggered when a download thread is completed.
        /// </summary>
        public event Action<IDownloadThread> ThreadCompleted;

        /// <summary>
        /// A reference to the list of download threads.
        /// </summary>
        private readonly List<IDownloadThread> s_threads;

        /// <summary>
        /// The download context that contains information about the download operation.
        /// </summary>
        private readonly IDownloadContext s_downloadContext;

        /// <summary>
        /// The delegate that will be executed by the download thread.
        /// </summary>
        private DownloadWorkDelegate s_work;

        /// <summary>
        /// The maximum number of threads that can be used for downloading.
        /// </summary>
        private readonly byte s_maxThreads;

        /// <summary>
        /// The factory for creating download threads.
        /// </summary>
        private readonly IDownloadThreadFactory s_factory;

        public DownloadThreadManager(IDownloadThreadFactory factory, IDownloadContext downloadContext, DownloadWorkDelegate work, byte maxThreads)
        {
            // Initialize the properties
            this.s_work = work;
            this.s_downloadContext = downloadContext;
            this.s_maxThreads = maxThreads;
            this.s_factory = factory;
            this.CompletedThreadsCount = 0;
        }

        /// <summary>
        /// Creates a new download thread.
        /// </summary>
        /// <returns>Whether the thread was created successfully or not.</returns>
        public Result<bool> CreateThread()
        {
            if (this.s_threads.Count > this.s_maxThreads) { Result<bool>.Failure("The number of download threads is at the maximum postition."); }
            // Create a new thread with the factory
            // Set the progresser for the thread
            // Add the thread to the list of threads
            IDownloadThread downloadThread = this.s_factory.Create(0, this.s_downloadContext, this.s_work);
            this.SetThreadProgresser(downloadThread);
            s_threads.Append(downloadThread);
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
                    this.ThreadCompleted?.Invoke(thread);
                    this.CompletedThreadsCount++;
                }
            });
            thread.SetProgresser(progresser);
        }

        /// <summary>
        /// Starts all download threads.
        /// </summary>
        public void Start()
        {
            foreach (IDownloadThread thread in s_threads)
            {
                thread.Start();
            }
        }

        /// <summary>
        /// Pauses all download threads.
        /// </summary>
        public void Pause()
        {
            foreach (IDownloadThread thread in s_threads)
            {
                thread.Pause();
            }
        }

        /// <summary>
        /// Resumes all download threads.
        /// </summary>
        public void Resume()
        {
            foreach (IDownloadThread thread in s_threads)
            {
                thread.Resume();
            }
        }

        /// <summary>
        /// Stops all download threads.
        /// </summary>
        public void Cancel()
        {
            foreach (IDownloadThread thread in s_threads)
            {
                thread.Cancel();
            }
        }

        /// <summary>
        /// Gets the list of download threads.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IDownloadThread> GetThreads() => s_threads;
    }
}