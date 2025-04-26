using Microsoft.Win32.SafeHandles;
using MultithreadDownload.IO;
using MultithreadDownload.Protocols;
using MultithreadDownload.Tasks;
using MultithreadDownload.Threading;
using MultithreadDownload.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Schedulers
{
    public class DownloadTaskWorkProvider : IDownloadTaskWorkProvider
    {
        /// <summary>
        /// Execute the main work of the download task.
        /// </summary>
        /// <param name="downloadService"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public Result<bool> Execute_MainWork(IDownloadService downloadService, DownloadTask task)
        {
            Result<Stream[]> inputStreams = downloadService.GetStreams(task.DownloadContext);
            if (!inputStreams.IsSuccess) { return Result<bool>.Failure("GetStream failed"); }
            Result<Stream[]> outputStreams = GetTaskStreams((byte)task.DownloadThreadManager.GetThreads().Count(), task.DownloadContext);
            if (!outputStreams.IsSuccess) { return Result<bool>.Failure("GetTaskStreams failed"); }
            task.DownloadThreadManager.Start(inputStreams.Value, outputStreams.Value);
            return Result<bool>.Success(true);
        }

        private Result<Stream[]> GetTaskStreams(byte maxParallelThreads, IDownloadContext downloadContext)
        {
            // Use GetUniqueFileName() to get a unique file name for the download context
            // for preventing file name conflicts then create the file streams for each thread
            string safePath = PathHelper.GetUniqueFileName(
                Path.GetDirectoryName(downloadContext.TargetPath), Path.GetFileName(downloadContext.TargetPath));
            Result<string[]> pathResult = FileSegmentHelper.SplitPaths(maxParallelThreads, safePath);
            if (!pathResult.IsSuccess) { return Result<Stream[]>.Failure("SplitPaths failed"); }
            Result<Stream[]> streamResult = CreateStreams(pathResult.Value);
            if (!streamResult.IsSuccess) { return Result<Stream[]>.Failure("SplitPaths failed"); }
            return streamResult;
        }

        /// <summary>
        /// Get the final stream for the download task.
        /// </summary>
        /// <param name="maxParallelThreads"></param>
        /// <param name="downloadContext"></param>
        /// <returns></returns>
        public Result<Stream> GetTaskFinalStream(IDownloadContext downloadContext)
        {
            // Use GetUniqueFileName() to get a unique file name for the download context
            // for preventing file name conflicts then create the file streams for each thread
            string safePath = PathHelper.GetUniqueFileName(
                Path.GetDirectoryName(downloadContext.TargetPath), Path.GetFileName(downloadContext.TargetPath));
            Result<Stream> streamResult = CreateStreams(safePath);
            if (!streamResult.IsSuccess) { return Result<Stream>.Failure("SplitPaths failed"); }
            return streamResult;
        }

        /// <summary>
        /// Create streams for each thread to write to.
        /// </summary>
        /// <param name="threadPaths">The paths of the threads.</param>
        /// <returns>The result of the operation.</returns>
        private Result<Stream[]> CreateStreams(string[] threadPaths)
        {
            // Create a stream for each thread path and return the streams in an array.
            Stream[] streams = new Stream[threadPaths.Length];
            try
            {
                for (int i = 0; i < streams.Length; i++)
                {
                    streams[i] = new FileStream(threadPaths[i], FileMode.Create);
                }
            }
            catch (Exception ex)
            {
                return Result<Stream[]>.Failure($"Create streams failed because {ex.Message}");
            }
            return Result<Stream[]>.Success(streams);
        }

        private Result<Stream> CreateStreams(string finalPath)
        {
            // Create a stream for the final path and return the stream.
            try
            {
                Stream stream = new FileStream(finalPath, FileMode.Create);
                return Result<Stream>.Success(stream);
            }
            catch (Exception ex)
            {
                return Result<Stream>.Failure($"Create streams failed because {ex.Message}");
            }
        }

        /// <summary>
        /// Finalize the work of the download task.
        /// </summary>
        /// <param name="outStream">The output stream to write to.</param>
        /// <param name="downloadService">The download service to use.</param>
        /// <param name="task">The download task to finalize.</param>
        /// <returns>Whether the finalization was successful or not.</returns>
        public Result<bool> Execute_FinalizeWork(Stream outStream,IDownloadService downloadService, DownloadTask task)
        {
            // Finalize the work of the download task.
            // This includes closing the streams and merging the files.
            Result<bool> result = downloadService.PostDownloadProcessing(outStream, task);
            if (!result.IsSuccess) { return Result<bool>.Failure("MergeFiles failed"); }
            return Result<bool>.Success(true);
        }
    }
}
