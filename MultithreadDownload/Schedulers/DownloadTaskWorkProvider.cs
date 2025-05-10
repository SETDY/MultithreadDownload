using MultithreadDownload.CoreTypes.Failures;
using MultithreadDownload.Protocols;
using MultithreadDownload.Tasks;
using MultithreadDownload.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;

namespace MultithreadDownload.Schedulers
{
    /// <summary>
    /// This class is used to build a relation between DownloadTask, DownloadThread and IDownloadService.
    /// </summary>
    public class DownloadTaskWorkProvider : IDownloadTaskWorkProvider
    {
        /// <summary>
        /// Execute the main work of the download task.
        /// </summary>
        /// <param name="downloadService"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public Result<bool, DownloadProcessFailure> Execute_MainWork(IDownloadService downloadService, DownloadTask task)
        {
            // This method performs a sequence of potentially fallible operations using the Result<T, E> pattern:
            // 1. It attempts to acquire the input streams needed to read from the remote source. This may fail due to network errors or invalid context.
            // 2. If successful, it then attempts to create output streams for writing to the local target file, partitioned by download threads. This may also fail, e.g., due to file system access errors.
            // 3. If both operations succeed, the method uses the DownloadThreadManager to start the download process using the paired input and output streams.
            // 
            // The use of chained AndThen and Map calls ensures that each step is only executed if the previous step was successful,
            // avoiding nested conditionals and improving readability. Errors are automatically propagated without throwing exceptions,
            // enabling a robust and testable error-handling workflow.
            // 
            // The method returns a Result<bool, DownloadProcessFailure> indicating either full success or the first encountered failure reason.
            return downloadService.GetStreams(task.DownloadContext)
                .AndThen(inputStreams =>
                    GetTaskStreams((byte)task.DownloadThreadManager.MaxParallelThreads, task.DownloadContext.TargetPath)
                    .Map(outputStreams =>
                    {
                        task.DownloadThreadManager.Start(inputStreams, outputStreams);
                        return true;
                    })
                );
        }

        /// <summary>
        /// Get the streams for each thread to write to.
        /// </summary>
        /// <param name="maxParallelThreads">The maximum number of parallel threads.</param>
        /// <param name="downloadContext">The download context to use.</param>
        /// <returns></returns>
        private Result<Stream[], DownloadProcessFailure> GetTaskStreams(byte maxParallelThreads, IDownloadContext context)
        {
            // Check whether the final file path is existed a file
            // for preventing file conflicts then create the file streams for each thread.
            // Caution:
            // Since Path.GetDirectoryName() will return null when the path is rooted on Windows,
            // PathHelper.GetDirectoryNameSafe() must be used to prevent null reference exception
            if (File.Exists(finalFilePath))
                return Result<Stream[], DownloadProcessFailure>.Failure(new DownloadProcessFailure(DownloadProcessFailureReason.FileAlreadyExisted, $"{finalFilePath} is already existed.", null));
            Result<string[], DownloadProcessFailure> pathResult = FileSegmentHelper.SplitPaths(maxParallelThreads, finalFilePath);
            Result<Stream[], DownloadProcessFailure> streamResult = pathResult.AndThen<Stream[]>(CreateStreams);
            return streamResult;
        }

        /// <summary>
        /// Get the final stream for the download task.
        /// </summary>
        /// <param name="maxParallelThreads"></param>
        /// <param name="downloadContext"></param>
        /// <returns></returns>
        public Result<Stream, DownloadProcessFailure> GetTaskFinalStream(IDownloadContext context)
        {
            // Check whether the final file path is existed a file
            // for preventing file conflicts then create the file streams for each thread.
            if (File.Exists(finalFilePath))
                return Result<Stream, DownloadProcessFailure>.Failure(new DownloadProcessFailure(DownloadProcessFailureReason.FileAlreadyExisted, $"{finalFilePath} is already existed.", null));
            return CreateStream(finalFilePath);
        }

        /// <summary>
        /// Create streams for each thread to write to.
        /// </summary>
        /// <param name="threadPaths">The paths of the threads.</param>
        /// <returns>The result of the operation.</returns>
        private Result<Stream[], DownloadProcessFailure> CreateStreams(string[] threadPaths)
        {
            // Using TryAll() to create all streams for each thread path which
            // will return a result of the operation, including a list of streams.
            // If the operation is failed, close all streams and return the failure.

            List<Stream> streams = new List<Stream>(threadPaths.Length);

            var result = Result<Stream, DownloadProcessFailure>.TryAll(threadPaths, path =>
            {
                Result<Stream, DownloadProcessFailure> streamResult = CreateStream(path);

                if (streamResult.IsSuccess)
                    streams.Add(streamResult.SuccessValue.Value!);

                return streamResult;
            });
            // If the result is failed, close all streams
            result.MatchFailure(x => streams.ForEach(stream => stream?.Close()));
            return result;
        }

        /// <summary>
        /// Create streams for each thread to write to.
        /// </summary>
        /// <param name="threadPath">The path of the threads.</param>
        /// <returns>The result of the operation.</returns>
        private Result<Stream, DownloadProcessFailure> CreateStream(string finalPath)
        {
            // Create a stream for the final path and return the stream.
            try
            {
                Stream stream = new FileStream(finalPath, FileMode.CreateNew);
                return Result<Stream, DownloadProcessFailure>.Success(stream);
            }
            catch (SecurityException ex)
            {
                return Result<Stream, DownloadProcessFailure>.Failure(new DownloadProcessFailure(DownloadProcessFailureReason.UnauthorisedAccessFailure,
                    $"When creating a stream for each of the path of threads, a unexpected sercurity exception is occured.", ex));
            }
            catch (IOException ex)
            {
                return Result<Stream, DownloadProcessFailure>.Failure(new DownloadProcessFailure(DownloadProcessFailureReason.IOFailure,
                    $"When creating a stream for each of the path of threads, a unexpected IO exception is occured.", ex));
            }
            catch (Exception ex)
            {
                return Result<Stream, DownloadProcessFailure>.Failure(new DownloadProcessFailure(DownloadProcessFailureReason.UnknownStreamFailure,
                    $"When creating a stream for each of the path of threads, a unexpected and unknown exception is occured.", ex));
            }
        }

        /// <summary>
        /// Finalize the work of the download task.
        /// </summary>
        /// <param name="outStream">The output stream to write to.</param>
        /// <param name="downloadService">The download service to use.</param>
        /// <param name="task">The download task to finalize.</param>
        /// <returns>Whether the finalization was successful or not.</returns>
        public Result<bool, DownloadProcessFailure> Execute_FinalizeWork(Stream outStream, IDownloadService downloadService, DownloadTask task)
        {
            // Finalize the work of the download task.
            // This includes closing the streams and merging the files.
            return downloadService.PostDownloadProcessing(outStream, task);;
        }
    }
}