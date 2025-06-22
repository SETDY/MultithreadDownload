using MultithreadDownload.Protocols;
using MultithreadDownload.Tasks;
using MultithreadDownload.Primitives;
using System;
using System.IO;
using MultithreadDownload.Core.Errors;
using MultithreadDownload.Logging;
using MultithreadDownload.Downloads;
using System.Collections.Generic;
using System.Linq;

namespace MultithreadDownload.Schedulers
{
    public class DownloadTaskWorkProvider : IDownloadTaskWorkProvider
    {
        /// <summary>
        /// Execute the main work of the download task.
        /// </summary>
        /// <param name="downloadService">The download service to use for downloading.</param>
        /// <param name="task">The download task to execute.</param>
        /// <returns>Whether the main work was successful or not.</returns>
        public Result<bool, DownloadError> Execute_MainWork(IDownloadService downloadService, DownloadTask task)
        {
            // Check if the download service and task are not null and if the task state is Waiting.
            if (downloadService is null) { return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.NullReference, "Parameter downloadService is null.")); }
            if (task is null) { return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.NullReference, "Parameter task is null.")); }
            if (task.State is not DownloadState.Downloading) { return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.NullReference, "The state of download task is not Waiting, causing main work cannot be excecuted.")); }
            // First, Get the input stream from the download service.
            // Then, get the output streams for each thread to write to if the input stream is successfully retrieved.
            // After that, start the threads with the input stream and output streams if the output streams are successfully retrieved.
            // If any of the steps fail, return a failure result with an appropriate error message.
            // Log the start of the main work execution.
            DownloadLogger.LogInfo($"Executing main work for download task: {task.DownloadContext.TargetPath}");
            return downloadService
                .GetStreams(task.DownloadContext)
                .AndThen(inputStream =>
                    GetTaskStreams(task.ThreadManager.MaxParallelThreads, task.DownloadContext)
                    .Map(outputStreams =>
                    {
                        task.ThreadManager.Start(inputStream, outputStreams);
                        return true;
                    }
                    )
                );
        }

        /// <summary>
        /// Get the streams for each thread to write to.
        /// </summary>
        /// <param name="maxParallelThreads">The maximum number of parallel threads.</param>
        /// <param name="downloadContext">The download context to use.</param>
        /// <returns></returns>
        private Result<Stream[], DownloadError> GetTaskStreams(byte maxParallelThreads, IDownloadContext downloadContext)
        {
            // Use GetUniqueFileName() to get a unique file name for the download context
            // for preventing file name conflicts then create the file streams for each thread.
            // Caution:
            // Since Path.GetDirectoryName() will return null when the path is rooted,
            // PathHelper.GetDirectoryNameSafe() must be used to prevent null reference exception
            string safePath = PathHelper.GetUniqueFileName(
                PathHelper.GetDirectoryNameSafe(downloadContext.TargetPath), Path.GetFileName(downloadContext.TargetPath));
            return FileSegmentHelper
                .SplitPaths(maxParallelThreads, safePath)
                .AndThen(CreateStreamsSafe);
        }

        /// <summary>
        /// Get the final stream for the download task.
        /// </summary>
        /// <param name="maxParallelThreads"></param>
        /// <param name="downloadContext"></param>
        /// <returns>The final file stream for the task to write to.</returns>
        public Result<Stream, DownloadError> GetTaskFinalStream(IDownloadContext downloadContext)
        {
            // Use GetUniqueFileName() to get a unique file name for the download context
            // for preventing file name conflicts then create the file streams for each thread
            string safePath = PathHelper.GetUniqueFileName(
                PathHelper.GetDirectoryNameSafe(downloadContext.TargetPath), Path.GetFileName(downloadContext.TargetPath));
            Result<Stream, DownloadError> streamResult = CreateStreamSafe(safePath);
            return streamResult;
        }

        /// <summary>
        /// Create streams for each thread to write to safely.
        /// </summary>
        /// <param name="threadPaths">The paths of the threads.</param>
        /// <returns>The result of the operation.</returns>
        private Result<Stream[], DownloadError> CreateStreamsSafe(string[] threadPaths)
        {
            IEnumerable<Result<Stream, DownloadError>> resultEnumerator =
                Enumerable.Range(0, threadPaths.Length)
                .Select(i =>
                {
                    return CreateStreamSafe(threadPaths[i]);
                });
            // Using enumerable to create streams for each thread path.
            return Result<Stream, DownloadError>.TryAll(resultEnumerator);
        }

        /// <summary>
        /// Create a stream for the given thread path safely.
        /// </summary>
        /// <param name="finalPath">The final path to create the stream for.</param>
        /// <returns>The stream for the given thread path.</returns>
        private Result<Stream, DownloadError> CreateStreamSafe(string finalPath)
        {
            // Create a stream for the final path and return the stream.
            try
            {
                Stream stream = new FileStream(finalPath, FileMode.Create);
                return Result<Stream, DownloadError>.Success(stream);
            }
            catch (UnauthorizedAccessException unEx)
            {
                return Result<Stream, DownloadError>.Failure(DownloadError.Create(
                    DownloadErrorCode.PermissionDenied, $"Create final stream failed because {unEx.Message}"));
            }
            catch (Exception ex)
            {
                return Result<Stream, DownloadError>.Failure(DownloadError.Create(
                    DownloadErrorCode.DiskOperationFailed, $"Create final stream failed because {ex.Message}"));
            }
        }

        /// <summary>
        /// Finalize the work of the download task.
        /// </summary>
        /// <param name="outStream">The output stream to write to.</param>
        /// <param name="downloadService">The download service to use.</param>
        /// <param name="task">The download task to finalize.</param>
        /// <returns>Whether the finalization was successful or not.</returns>
        public Result<bool, DownloadError> Execute_FinalizeWork(Stream outStream, IDownloadService downloadService, DownloadTask task)
        {
            // Finalize the work of the download task.
            // This includes closing the streams and merging the files.
            return downloadService.PostDownloadProcessing(outStream, task);
        }
    }
}