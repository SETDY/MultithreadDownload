using MultithreadDownload.Protocols;
using MultithreadDownload.Tasks;
using MultithreadDownload.Utils;
using System;
using System.IO;

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
            Result<Stream[]> outputStreams = GetTaskStreams((byte)task.ThreadManager.MaxParallelThreads, task.DownloadContext);
            if (!outputStreams.IsSuccess) { return Result<bool>.Failure("GetTaskStreams failed"); }


            task.ThreadManager.Start(inputStreams.Value, outputStreams.Value);
            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Get the streams for each thread to write to.
        /// </summary>
        /// <param name="maxParallelThreads">The maximum number of parallel threads.</param>
        /// <param name="downloadContext">The download context to use.</param>
        /// <returns></returns>
        private Result<Stream[]> GetTaskStreams(byte maxParallelThreads, IDownloadContext downloadContext)
        {
            // Use GetUniqueFileName() to get a unique file name for the download context
            // for preventing file name conflicts then create the file streams for each thread.
            // Caution:
            // Since Path.GetDirectoryName() will return null when the path is rooted,
            // PathHelper.GetDirectoryNameSafe() must be used to prevent null reference exception
            string safePath = PathHelper.GetUniqueFileName(
                PathHelper.GetDirectoryNameSafe(downloadContext.TargetPath), Path.GetFileName(downloadContext.TargetPath));
            Result<string[]> fileSegmentPaths = FileSegmentHelper.SplitPaths(maxParallelThreads, safePath);
            if (!fileSegmentPaths.IsSuccess) { return Result<Stream[]>.Failure("SplitPaths failed"); }
            Result<Stream[]> streamResult = CreateStreams(fileSegmentPaths.Value);
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
                PathHelper.GetDirectoryNameSafe(downloadContext.TargetPath), Path.GetFileName(downloadContext.TargetPath));
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
        public Result<bool> Execute_FinalizeWork(Stream outStream, IDownloadService downloadService, DownloadTask task)
        {
            // Finalize the work of the download task.
            // This includes closing the streams and merging the files.
            Result<bool> result = downloadService.PostDownloadProcessing(outStream, task);
            if (!result.IsSuccess) { return Result<bool>.Failure("MergeFiles failed"); }
            return Result<bool>.Success(true);
        }
    }
}