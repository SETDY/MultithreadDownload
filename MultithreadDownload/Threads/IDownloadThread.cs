using MultithreadDownload.Downloads;
using MultithreadDownload.Protocols;
using MultithreadDownload.Utils;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MultithreadDownload.Threads
{
    public interface IDownloadThread : IDisposable
    {
        /// <summary>
        /// The ID of the download thread. This is used to identify the thread in the download task.
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// The size of the file that has been downloaded by this thread.
        /// </summary>
        public long CompletedBytesSizeCount { get; }

        public IDownloadContext DownloadContext { get; }

        /// <summary>
        /// The current state of the download thread.
        /// </summary>
        DownloadState State { get; }

        /// <summary>
        /// The status of the download thread.
        /// </summary>
        public bool IsAlive { get; }

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
        void Start(Stream inputStream, Stream outputStream);

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

        internal void SetState(DownloadState state);

        void SetDownloadProgress(sbyte progress);

        internal void AddCompletedBytesSizeCount(long count);
    }
}