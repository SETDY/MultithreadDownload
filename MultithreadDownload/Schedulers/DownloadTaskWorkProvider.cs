using Microsoft.Win32.SafeHandles;
using MultithreadDownload.Core;
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
    public class DownloadTaskWorkProvider
    {
        public async Task<Result<bool>> Execute(IDownloadService downloadService, DownloadTask task)
        {
            Result<Stream> inputStream = downloadService.GetStream(task.DownloadContext);
            if (!inputStream.IsSuccess) { return Result<bool>.Failure("GetStream failed"); }
            Result<Stream[]> outputStream = GetTaskStreams((byte)task.DownloadThreadManager.GetThreads().Count(), task.DownloadContext);
            if (!outputStream.IsSuccess) { return Result<bool>.Failure("GetTaskStreams failed"); }
            task.DownloadThreadManager.Start(inputStream.Value, outputStream.Value);
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
    }
}
