using MultithreadDownload.Downloads;
using MultithreadDownload.Utils;
using System;
using System.Threading.Tasks;

namespace MultithreadDownload.Threads
{
    public interface IDownloadThread
    {
        /// <summary>
        /// The size of the file that has been downloaded by this thread.
        /// </summary>
        public long CompletedBytesSizeCount { get; }

        /// <summary>
        /// The current state of the download thread.
        /// </summary>
        DownloadTaskState State { get; }

        /// <summary>
        /// The path to the file segment that this thread is responsible for downloading.
        /// </summary>
        public string FileSegmentPath { get; set; }

        /// <summary>
        /// The task that will execute the download operation.
        /// </summary>
        public Task WorkerTask { get; set; }

        /// <summary>
        /// The download progress of the file that has been downloaded by this thread.
        /// </summary>
        IProgress<sbyte> Progresser { get; }

        /// <summary>
        /// Start newly the download thread.
        /// </summary>
        void Start();

        /// <summary>
        /// Pause the download thread.
        /// </summary>
        void Pause();

        /// <summary>
        /// Resume the download thread.
        /// </summary>
        void Resume();

        /// <summary>
        /// Cancel the download thread.
        /// </summary>
        Result<bool> Cancel();

        void SetProgresser(IProgress<sbyte> progresser);
    }
}