using MultithreadDownload.Downloads;
using MultithreadDownload.Tasks;
using MultithreadDownload.Threads;
using MultithreadDownload.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace MultithreadDownload.Protocols
{
    /// <summary>
    /// HttpDownloadService is a class that implements IDownloadService interface
    /// which is for downloading file through http.
    /// </summary>
    public class HttpDownloadService : IDownloadService
    {
        #region Implement of GetStreams method

        /// <summary>
        /// Max retry times
        /// </summary>
        private const uint MAX_RETRY = 3;

        /// <summary>
        /// The wait time between retries in milliseconds
        /// </summary>
        private const int WAIT_TIME = 5000;

        /// <summary>
        /// Get the streams for each of the download threads of the download task.
        /// </summary>
        /// <param name="downloadContext"></param>
        /// <param name="rangePostions"></param>
        /// <returns>The streams for each of the download threads of the download task</returns>
        public Result<Stream[]> GetStreams(IDownloadContext downloadContext)
        {
            Stream[] streams = new Stream[downloadContext.RangePositions.Length];
            for (int i = 0; i < downloadContext.RangePositions.GetLength(0); i++)
            {
                // Get the start and end position of the file to be downloaded
                long startPosition = downloadContext.RangePositions[i, 0];
                long endPosition = downloadContext.RangePositions[i, 1];
                // Get the stream from the download context
                Result<Stream> result = GetStream(downloadContext, startPosition, endPosition);
                if (!result.IsSuccess)
                {
                    Debug.WriteLine($"Get a error message from the request: {result.ErrorMessage} which is from url: {((HttpDownloadContext)downloadContext).Url}");
                    return Result<Stream[]>.Failure($"Thread cannot connect to {((HttpDownloadContext)downloadContext).Url}");
                }
                streams[i] = result.Value;
            }
            return Result<Stream[]>.Success(streams);
        }

        /// <summary>
        /// Get the stream for the download thread of the download task.
        /// </summary>
        /// <param name="downloadContext">The download context</param>
        /// <param name="startPosition">The start position of the file to be downloaded</param>
        /// <param name="endPosition">The end position of the file to be downloaded</param>
        /// <returns></returns>
        private Result<Stream> GetStream(IDownloadContext downloadContext, long startPosition, long endPosition)
        {
            // Get the download context from the download thread.
            // Since the class is about Http, we can just cast it to HttpDownloadContext.
            HttpDownloadContext httpDownloadContext = (HttpDownloadContext)downloadContext;
            // Get a new httpClient instance.
            // Create a request message for a new httpClient instance.
            // Send a request and get the response.
            HttpClient client = HttpClientPool.GetClient();
            HttpRequestMessage requestMessage = CreateRequestMessage(
                httpDownloadContext.Url,
                startPosition,
                endPosition
                );
            Result<HttpResponseMessage> result = SendRequestSafe(
                client,
                requestMessage
                );
            // Exception handling:
            // If the request is successful, return the response stream.
            // Otherwise, return a failure result with the error message.
            if (!result.IsSuccess)
            {
                Debug.WriteLine($"Get a error message from the request: {result.ErrorMessage} which is from url: {httpDownloadContext.Url}");
                return Result<Stream>.Failure($"Thread  cannot connect to {httpDownloadContext.Url}");
            }
            return Result<Stream>.Success(result.Value.Content.ReadAsStream());
        }

        private HttpRequestMessage CreateRequestMessage(string url, long startDownloadPosition, long endDownloadPosition)
        {
            // Set the method which is Get for the request and the offset of the requsted file.
            HttpRequestMessage httpRequestMessage =
                new HttpRequestMessage(HttpMethod.Get, url);
            httpRequestMessage.Headers.Range =
                new RangeHeaderValue(startDownloadPosition, endDownloadPosition);
            return httpRequestMessage;
        }

        /// <summary>
        /// Send the request and get the response safely
        /// </summary>
        /// <param name="client">A HttpClient instance</param>
        /// <param name="requestMessage">A HttpRequestMessage instance</param>
        /// <returns>Result<HttpResponseMessage></returns>
        private Result<HttpResponseMessage> SendRequestSafe(HttpClient client, HttpRequestMessage requestMessage)
        {
            // Try to send the request and get the response
            // If failed, retry for a maximum of MAX_RETRY times
            // Retrun Result<HttpResponseMessage> so that the exception must be handled by the caller
            for (int i = 0; i < MAX_RETRY; i++)
            {
                Result<HttpResponseMessage> result = SendRequest(client, requestMessage);
                // If the request is successful, return the response.
                // Otherwise, wait for WAIT_TIME milliseconds and retry.
                if (!result.IsSuccess)
                {
                    Thread.Sleep(WAIT_TIME);
                    continue;
                }
                else
                {
                    return Result<HttpResponseMessage>.Success(result.Value);
                }
            }
            return Result<HttpResponseMessage>.Failure("Failed to send request after multiple attempts.");
        }

        /// <summary>
        /// Send the request and get the response
        /// </summary>
        /// <param name="client">A HttpClient instance</param>
        /// <param name="requestMessage">A HttpRequestMessage instance</param>
        /// <returns>Result<HttpResponseMessage></returns>
        private Result<HttpResponseMessage> SendRequest(HttpClient client, HttpRequestMessage requestMessage)
        {
            // Try to send the request and get the response
            // Retrun Result<HttpResponseMessage> so that the exception must be handled by the caller
            try
            {
                // Set the timeout for the request
                using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(WAIT_TIME)))
                {
                    // Send a request and get the response
                    // Check if the response is successful
                    HttpResponseMessage responseMessage = client.Send(requestMessage);
                    responseMessage.EnsureSuccessStatusCode();
                    return Result<HttpResponseMessage>.Success(responseMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Request failed: {ex.Message}");
                return Result<HttpResponseMessage>.Failure($"Request failed: {ex.Message}");
            }
        }

        #endregion Implement of GetStreams method

        public Result<int> DownloadFile(Stream inputStream, Stream outputStream, IDownloadThread downloadThread)
        {
            // Download the file from the input stream(Internet) to the output stream(Local drive).
            // If the download is successful, get the bytes of file and write it into file
            // and return the number of bytes written.
            // Otherwise, retry for a maximum of TRYTIMES times
            // If still failed, return a failure result with the error message.
            int readBytesCount = 0;
            byte[] fileBytes = new byte[4096];
            downloadThread.SetState(DownloadState.Downloading);
            for (int tryCount = 0; tryCount < MAX_RETRY && downloadThread.State == DownloadState.Downloading; tryCount++)
            {
                try
                {
                    readBytesCount = inputStream.Read(fileBytes, 0, fileBytes.Length);
                    outputStream.Write(fileBytes, 0, readBytesCount);
                    this.SetCompletedByteNumbers(downloadThread, readBytesCount);
                    return Result<int>.Success(readBytesCount);
                }
                catch (Exception)
                {
                    Debug.WriteLine(
                        $"Thread failed to read data, " +
                        $"waiting {WAIT_TIME / 1000} seconds before retrying " +
                        $"for {((HttpDownloadContext)downloadThread.DownloadContext).Url}");
                    Thread.Sleep(WAIT_TIME);
                    tryCount++;
                    continue;
                }
            }
            // If the download is still failed after MAX_RETRY times or user cancel the download task
            // Clean up the download progress by closing and disposing the output stream and
            // return a failure result with the error message.
            CleanDownloadProgess(outputStream, null);
            return Result<int>.Failure(
                $"While thread failed to read data after {MAX_RETRY} attempts, " +
                $"the download task is cancelled for {((HttpDownloadContext)downloadThread.DownloadContext).Url}");
        }

        /// <summary>
        /// Set the completed byte numbers for the download thread
        /// </summary>
        /// <param name="downloadThread">The thread that is downloading the file</param>
        /// <param name="readBytesCount">The number of bytes that have been read</param>
        private void SetCompletedByteNumbers(IDownloadThread downloadThread, int readBytesCount)
        {
            // Add the completed byte numbers for the download thread
            // Set the download progress for the download thread
            downloadThread.AddCompletedBytesSizeCount(readBytesCount);
            // This variable is used to simply calculate the download progress
            // because it is too long that writing the whole expression.
            long threadDownloadSize =
                ((HttpDownloadContext)downloadThread.DownloadContext).RangePositions[downloadThread.ID, 1] -
                ((HttpDownloadContext)downloadThread.DownloadContext).RangePositions[downloadThread.ID, 0];
            downloadThread.SetDownloadProgress(
                (sbyte)(downloadThread.CompletedBytesSizeCount / threadDownloadSize * 100));
        }

        public Result<bool> PostDownloadProcessing(Stream outputStream, DownloadTask task)
        {
            // If the worker thread is not alive, it means that the download task has been cancelled
            // Clean up the download progress by closing and disposing the output stream and delete the file
            // However, if the worker thread is alive, it means that the download task is completed
            // In this case, we need to clean up the download progress by closing and disposing the output stream
            // but do not delete the file
            // After that, we need to combine the segments of the file to a single file
            if (task.DownloadThreadManager.CompletedThreadsCount != task.DownloadThreadManager.MaxParallelThreads)
            {
                this.CleanDownloadProgess(outputStream,
                    task.DownloadThreadManager.GetThreads().Select(x => x.FileSegmentPath).ToArray());
                return Result<bool>.Failure(
                    $"Task {task.ID} does not be completed and cannot do PostDownloadProcessing().Therefore, The task has be cancelled");
            }

            Result<bool> result = FileSegmentHelper.CombineSegmentsSafe(
                task.DownloadThreadManager.GetThreads().Select(x => x.FileSegmentPath).ToArray(),
                task.DownloadContext.TargetPath
            );
            if (!result.IsSuccess)
            {
                Debug.WriteLine($"Failed to combine segments: {result.ErrorMessage}");
                return Result<bool>.Failure($"Failed to combine segments: {result.ErrorMessage}");
            }
            this.CleanDownloadProgess(outputStream);
            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Clean up the download progress by closing and disposing the output stream
        /// </summary>
        /// <param name="targettream">The stream you want to clean</param>
        /// <param name="filePath">The path of the downloading file</param>
        /// <returns>Whether the operation is success or not</returns>
        private Result<bool> CleanDownloadProgess(Stream targetStream)
        {
            // Clean up the download progress by closing and disposing the output stream
            // If the filePath is not null, delete the file
            // for why the filePath is null, it means that the download is not completed
            // e.g. The download only executed to DownloadFile().
            // Return a success result if the operation is successful
            if (targetStream == null) { return Result<bool>.Failure("Output stream is null"); }
            try
            {
                targetStream.Flush();
                targetStream.Close();
                targetStream.Dispose();
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to clean download progress: {ex.Message}");
                return Result<bool>.Failure($"Failed to clean download progress: {ex.Message}");
            }
        }

        private Result<bool> CleanDownloadProgess(Stream targetStream, string[] filePath)
        {
            // Clean up the download progress by closing and disposing the output stream
            // If the filePath is not null, delete the file
            // for why the filePath is null, it means that the download is not completed
            // e.g. The download only executed to DownloadFile().
            // Return a success result if the operation is successful
            if (targetStream == null) { return Result<bool>.Failure("Output stream is null"); }
            try
            {
                targetStream.Flush();
                targetStream.Close();
                targetStream.Dispose();
                foreach (string path in filePath)
                {
                    File.Delete(path);
                }
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to clean download progress: {ex.Message}");
                return Result<bool>.Failure($"Failed to clean download progress: {ex.Message}");
            }
        }
    }
}