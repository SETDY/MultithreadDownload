using MultithreadDownload.Protocols;
using MultithreadDownload.Tasks;
using MultithreadDownload.Primitives;
using System.IO;
using MultithreadDownload.Core.Errors;

namespace MultithreadDownload.Schedulers
{
    public interface IDownloadTaskWorkProvider
    {
        /// <summary>
        /// Execute the main work of the download task.
        /// </summary>
        /// <param name="downloadService">The download service to use for downloading.</param>
        /// <param name="task">The download task to execute.</param>
        /// <returns>Whether the main work was successful or not.</returns>
        public Result<bool, DownloadError> Execute_MainWork(IDownloadService downloadService, DownloadTask task);

        /// <summary>
        /// Get the final stream for the download task.
        /// </summary>
        /// <param name="maxParallelThreads"></param>
        /// <param name="downloadContext"></param>
        /// <returns>The final file stream for the task to write to.</returns>
        public Result<Stream, DownloadError> GetTaskFinalStream(IDownloadContext downloadContext);

        /// <summary>
        /// Finalize the work of the download task.
        /// </summary>
        /// <param name="outStream">The output stream to write to.</param>
        /// <param name="downloadService">The download service to use.</param>
        /// <param name="task">The download task to finalize.</param>
        /// <returns>Whether the finalization was successful or not.</returns>
        public Result<bool, DownloadError> Execute_FinalizeWork(Stream outStream, IDownloadService downloadService, DownloadTask task);
    }
}