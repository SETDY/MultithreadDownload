using MultithreadDownload.Core;
using MultithreadDownload.Protocols;
using MultithreadDownload.Threads;
using MultithreadDownload.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Threading
{
    public class DownloadThreadManager : IDownloadThreadManager
    {
        /// <summary>
        /// The number of completed threads.
        /// </summary>
        public byte CompletedThreadsCount { get; private set; }

        /// <summary>
        /// The number of completed threads.
        /// </summary>
        public byte MaxParallelThreads
        {
            get
            {
                return this.s_maxThreads;
            }
        }

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
        /// The maximum number of threads that can be used for downloading.
        /// </summary>
        private readonly byte s_maxThreads;

        /// <summary>
        /// The factory for creating download threads.
        /// </summary>
        private readonly IDownloadThreadFactory s_factory;

        /// <summary>
        /// Constructor for the DownloadThreadManager class.
        /// </summary>
        /// <param name="factory">The factory for creating download threads.</param>
        /// <param name="downloadContext">The context that contains information about the download operation.</param>
        /// <param name="maxThreads">The maximum number of threads that can be used for downloading.</param>
        public DownloadThreadManager(IDownloadThreadFactory factory, byte maxThreads, IDownloadContext downloadContext)
        {
            // Initialize the properties
            this.s_downloadContext = downloadContext;
            this.s_maxThreads = maxThreads;
            this.s_factory = factory;
            this.CompletedThreadsCount = 0;
        }

        /// <summary>
        /// Creates a new download thread.
        /// </summary>
        /// <returns>Whether the thread was created successfully or not.</returns>
        /// <remarks>
        /// The work delegate is the main download work that will be executed by the download thread.
        /// The main download work is IDownloadSerivce.DownloadFile()
        /// </remarks>
        public Result<bool> CreateThread(Action mainWork)
        {
            if (this.s_threads.Count > this.s_maxThreads) { Result<bool>.Failure("The number of download threads is at the maximum postition."); }
            // Create a new thread with the factory
            // Set the progresser for the thread
            // Add the thread to the list of threads
            IDownloadThread downloadThread = this.s_factory.Create(0, this.s_downloadContext, mainWork);
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
        /// <param name="inputStream">The input stream to read from.</param>
        /// <param name="outputStreams">The output streams of each of threads to write to.</param>
        public void Start(Stream inputStream, Stream[] outputStreams)
        {
            // If the length of the output streams is not equal to the number of threads, throw an exception
            // Otherwise, start each thread with the input stream and the corresponding output stream
            if (outputStreams.Length != this.s_threads.Count) { throw new ArgumentException("The number of output streams must be equal to the number of threads."); }
            for (int i = 0; i < outputStreams.Length; i++)
            {
                this.s_threads[i].Start(inputStream, outputStreams[i]);
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