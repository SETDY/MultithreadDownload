using MultithreadDownload.Protocols.Abstractions;
using MultithreadDownload.Threads.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MultithreadDownload.Threads.Managers
{
    public class DownloadThreadManager<TTarget> where TTarget : IDownloadTarget
    {
        /// <summary>
        /// The download target about the download operation
        /// </summary>
        public TTarget Target { get; }

        /// <summary>
        /// The maximum number of threads that can be used for downloading.
        /// </summary>
        public byte MaximumThreadsCount { get; }

        /// <summary>
        /// The maximum nubmer of thread that can run in parallel.
        /// </summary>
        public byte MaximumParallelThreadsCount { get; }

        /// <summary>
        /// The list that contains all instances of download thread.
        /// </summary>
        public List<IDownloadThread<TTarget>> DownloadThreads { get; } = new List<IDownloadThread<TTarget>>();

        private IDownloadThreadFactory<TTarget> ThreadFactory { get; }


        /// <summary>
        /// Constructor for the ThreadManager class.
        /// </summary>
        /// <param name="factory">The factory for creating download threads.</param>
        /// <param name="target">The target that contains information about the download operation.</param>
        /// <param name="maxThreadsCount">The maximum number of threads that can be used for downloading.</param>
        /// <param name="maxParallelThreadsCount">The maximum number of threads that can run in parallel.</param>
        public DownloadThreadManager(IDownloadThreadFactory factory, TTarget target, byte maxThreadsCount, byte maxParallelThreadsCount)
        {
            // Initialize the properties
            this.Target = target;
            this.ThreadFactory = factory;
            this.MaximumThreadsCount = maxThreadsCount;
            this.MaximumParallelThreadsCount = maxParallelThreadsCount;
        }

        public void CreateThreads()
        {
            // Create the download threads using the factory
            for (byte i = 0; i < MaximumThreadsCount; i++)
            {
                IDownloadThread<TTarget> thread = ThreadFactory.Create(Target, );
                DownloadThreads.Add(thread);
            }
        }

        private void CreateThreadContext(long startPos, )
        {

        }
    }
}
