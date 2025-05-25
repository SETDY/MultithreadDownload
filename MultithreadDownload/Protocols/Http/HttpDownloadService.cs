using MultithreadDownload.Downloads;
using MultithreadDownload.Logging;
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

namespace MultithreadDownload.Protocols.Http
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
            Stream[] streams = new Stream[downloadContext.RangePositions.GetLength(0)];
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
                    // Send a request and get a streaming response that will update the response by the time passed.
                    // Check if the response is successful
                    // If not, throw an exception.
                    HttpResponseMessage responseMessage = client
                        .SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead)
                        .GetAwaiter().GetResult();
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
        #region Implement of DownloadFile method
        public Result<bool> DownloadFile(Stream inputStream, Stream outputStream, IDownloadThread downloadThread)
        {
            // Using HttpFileDownloader, download the file from the input stream(Internet) to the output stream(Local drive).
            HttpFileDownloader fileDownloader = new HttpFileDownloader(inputStream, outputStream, downloadThread);
            return fileDownloader.DownloadFile();
        }
        #endregion Implement of DownloadFile method

        public Result<bool> PostDownloadProcessing(Stream outputStream, DownloadTask task)
        {
            // If the worker thread is not alive, it means that the download task has been cancelled
            // Clean up the download progress by closing and disposing the output stream and delete the file
            // However, if the worker thread is alive, it means that the download task is completed
            // In this case, we need to clean up the download progress by closing and disposing the output stream
            // but do not delete the file
            // After that, we need to combine the segments of the file to a single file
            if (task.ThreadManager.CompletedThreadsCount != task.ThreadManager.MaxParallelThreads)
            {
                CleanDownloadProgess(outputStream,
                    task.ThreadManager.GetThreads().Select(x => x.FileSegmentPath).ToArray());
                return Result<bool>.Failure(
                    $"Task {task.ID} does not be completed and cannot do PostDownloadProcessing().Therefore, The task has be cancelled");
            }
            // This line is used to use ref keyword to pass the outputStream to the CombineSegmentsSafe method
            FileStream finalFileStream = outputStream as FileStream;
            Result<bool> result = FileSegmentHelper.CombineSegmentsSafe(
                task.ThreadManager.GetThreads().Select(x => x.FileSegmentPath).ToArray(),
                ref finalFileStream
            );
            if (!result.IsSuccess)
            {
                Debug.WriteLine($"Failed to combine segments: {result.ErrorMessage}");
                return Result<bool>.Failure($"Failed to combine segments: {result.ErrorMessage}");
            }
            CleanDownloadProgess(outputStream);
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

        private Result<bool> CleanDownloadProgess(Stream targetStream, string[] filePaths)
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
                // Delete the file if the filePath is not null
                if (filePaths != null)
                    filePaths.ToList().ForEach(path => File.Delete(path));
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