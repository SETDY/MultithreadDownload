using MultithreadDownload.Protocols;
using MultithreadDownload.Tasks;
using MultithreadDownload.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Schedulers
{
    public interface IDownloadTaskWorkProvider
    {
        /// <summary>
        /// Execute the main work of the download task.
        /// </summary>
        /// <param name="downloadService"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public Result<bool> Execute_MainWork(IDownloadService downloadService, DownloadTask task);

        /// <summary>
        /// Get the final stream for the download task.
        /// </summary>
        /// <param name="maxParallelThreads"></param>
        /// <param name="downloadContext"></param>
        /// <returns></returns>
        public Result<Stream> GetTaskFinalStream(IDownloadContext downloadContext);

        /// <summary>
        /// Finalize the work of the download task.
        /// </summary>
        /// <param name="outStream">The output stream to write to.</param>
        /// <param name="downloadService">The download service to use.</param>
        /// <param name="task">The download task to finalize.</param>
        /// <returns>Whether the finalization was successful or not.</returns>
        public Result<bool> Execute_FinalizeWork(Stream outStream, IDownloadService downloadService, DownloadTask task);
    }
}
