using MultithreadDownload.Core.Errors;
using MultithreadDownload.Downloads;
using MultithreadDownload.Logging;
using MultithreadDownload.Tasks;
using MultithreadDownload.Threads;
using MultithreadDownload.Primitives;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using MultithreadDownload.Utils;
using System.Collections.Generic;
using System.Collections;

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
        public Result<Stream[], DownloadError> GetStreams(IDownloadContext downloadContext)
        {
            // Set the enumerator for the range positions of the download context
            // to get the streams for each of the download threads of the download task.
            IEnumerable<Result<Stream, DownloadError>> resultEnumerator = 
                Enumerable.Range(0, downloadContext.RangePositions.GetLength(0))
                .Select(i =>
                {
                    long start = downloadContext.RangePositions[i, 0];
                    long end = downloadContext.RangePositions[i, 1];
                    return GetStreamSafe(downloadContext, start, end);
                });
            // Using the TryAll method to get the result of streams for each of the download threads of the download task.
            return Result<Stream, DownloadError>.TryAll(resultEnumerator);
        }

        /// <summary>
        /// Get the stream for the download thread of the download task safely.
        /// </summary>
        /// <param name="downloadContext">The download context</param>
        /// <param name="startPosition">The start position of the file to be downloaded</param>
        /// <param name="endPosition">The end position of the file to be downloaded</param>
        /// <returns>The result of getting the stream</returns>
        /// <remarks>
        /// This method is used to handle the exceptions that may occur when getting the stream.
        /// Therefore, Should use this method instead of GetStream directly.
        /// </remarks>
        private Result<Stream, DownloadError> GetStreamSafe(IDownloadContext downloadContext, long startPosition, long endPosition)
        {
            try
            {
                return GetStream(downloadContext, startPosition, endPosition);
            }
            catch (HttpRequestException httpEx)
            {
                return Result<Stream, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.HttpError, $"GetStream failed for an http request exception. Message: {httpEx.Message}"));
            }
            catch (ArgumentOutOfRangeException argEx)
            {
                return Result<Stream, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.TaskContextInvalid, $"GetStream failed for an invalid parameter from the context. Message: {argEx.Message}"));
            }
            catch (Exception ex)
            {
                return Result<Stream, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.UnexpectedOrUnknownException, $"GetStream failed for an unexpected exception. Message: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get the stream for the download thread of the download task.
        /// </summary>
        /// <param name="downloadContext">The download context</param>
        /// <param name="startPosition">The start position of the file to be downloaded</param>
        /// <param name="endPosition">The end position of the file to be downloaded</param>
        /// <returns></returns>
        private Result<Stream, DownloadError> GetStream(IDownloadContext downloadContext, long startPosition, long endPosition)
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

            // If the request is successful, return the response stream.
            // Otherwise, return a failure result with the same error message.
            return SendRequestWithRetry(client, requestMessage).Map(response => response.Content.ReadAsStream());

        }

        private HttpRequestMessage CreateRequestMessage(string url, long startDownloadPosition, long endDownloadPosition)
        {
            // Validate the parameters
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException("Url cannot be empty or null");
            if (startDownloadPosition < 0 || endDownloadPosition < 0 || startDownloadPosition > endDownloadPosition)
                throw new ArgumentOutOfRangeException("Invalid download position");

            // Set the method which is Get for the request and the offset of the requsted file.
            HttpRequestMessage httpRequestMessage =
                new HttpRequestMessage(HttpMethod.Get, url);
            httpRequestMessage.Headers.Range =
                new RangeHeaderValue(startDownloadPosition, endDownloadPosition);
            return httpRequestMessage;
        }

        /// <summary>
        /// Send the request and get the response safely and with retry mechanism
        /// </summary>
        /// <param name="client">A HttpClient instance</param>
        /// <param name="requestMessage">A HttpRequestMessage instance</param>
        /// <returns>Result<HttpResponseMessage></returns>
        private Result<HttpResponseMessage, DownloadError> SendRequestWithRetry(HttpClient client, HttpRequestMessage requestMessage)
        {
            // FIXED: Modify the lambda expression to ensure proper handling of async methods
            return RetryHelper.Retry<HttpResponseMessage, DownloadError>(
                (int)MAX_RETRY,
                WAIT_TIME,
                () => SendRequestSafe(client, requestMessage).Result, // Here use .Result to unwrap the Task and return the Result
                () => DownloadError.Create(DownloadErrorCode.Timeout, $"Http request timed out after {WAIT_TIME * MAX_RETRY}.")
            );
        }

        /// <summary>
        /// Send the request and get the response safely
        /// </summary>
        /// <param name="client">A HttpClient instance</param>
        /// <param name="requestMessage">A HttpRequestMessage instance</param>
        /// <returns>The result of sending the request and getting the response</returns>
        private async Task<Result<HttpResponseMessage, DownloadError>> SendRequestSafe(HttpClient client, HttpRequestMessage requestMessage)
        {
            // Try to send the request and get the response
            // Retrun Result<HttpResponseMessage> so that the exception must be handled by the caller
            try
            {
                // Use await keyword to wait for the response
                return await SendRequest(client, requestMessage);
            }
            catch (TaskCanceledException)
            {
                return Result<HttpResponseMessage, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.Timeout, $"Http request timed out after {WAIT_TIME}."));
            }
            catch (HttpRequestException httpEx)
            {
                return Result<HttpResponseMessage, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.HttpError, $"Http request failed: {httpEx.Message}"));
            }
            catch (Exception ex)
            {
                return Result<HttpResponseMessage, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.UnexpectedOrUnknownException, $"Http request failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Send the request and get the response
        /// </summary>
        /// <param name="client">The http client</param>
        /// <param name="requestMessage">The http request message</param>
        /// <returns>The result of sending the request and getting the response</returns>
        private async Task<Result<HttpResponseMessage, DownloadError>> SendRequest(HttpClient client, HttpRequestMessage requestMessage)
        {
            // Set the timeout for the request
            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(WAIT_TIME)))
            {
                // Send a request and get a streaming response that will update the response by the time passed.
                // Check if the response is successful
                // If not, throw an exception.
                try
                {
                    HttpResponseMessage responseMessage = await client
                        .SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
                    responseMessage.EnsureSuccessStatusCode();
                    return Result<HttpResponseMessage, DownloadError>.Success(responseMessage);
                }
                catch (TaskCanceledException)
                {
                    return Result<HttpResponseMessage, DownloadError>.Failure(DownloadError.Create(
                        DownloadErrorCode.Timeout, $"The request to {requestMessage.RequestUri.AbsoluteUri} timed out."));
                }
                catch (Exception ex)
                {
                    return Result<HttpResponseMessage, DownloadError>.Failure(DownloadError.Create(
                        DownloadErrorCode.UnexpectedOrUnknownException, $"An unexpected exception occurs when send the request or get the response. Message: {ex.Message}"));
                }
            }
        }

        #endregion Implement of GetStreams method

        #region Implement of DownloadFile method
        public Result<bool, DownloadError> DownloadFile(Stream inputDownloadingStream, Stream outputWritingStream, IDownloadThread processingDownloadThread)
        {
            // Using HttpFileDownloader, download the file from the input stream(Internet) to the output stream(Local drive).
            HttpFileDownloader fileDownloader = new HttpFileDownloader
                (inputDownloadingStream, outputWritingStream, processingDownloadThread);
            // Download the file and return the result.
            return fileDownloader.DownloadFile();
        }
        #endregion Implement of DownloadFile method

        #region Implement of PostDownloadProcessing method
        public Result<bool, DownloadError> PostDownloadProcessing(Stream outputStream, DownloadTask task)
        {
            // Firstly, Check the state of the task and the completed threads count.
            // If the task is cancelled, clean up the download progress by closing and disposing the output stream and delete the file segments.
            // If the completed threads count is not equal to the max parallel threads count, take care of the situation that the task has been cancelled. (For explaination, see the method ValidateOrCleanup below.)
            // Secondly, combine the segments of the file to a single file.
            // Finally, clean up the download progress by closing and disposing the output stream and delete the file segments with logging.
            return ValidateOrCleanup(task, outputStream)
                   .AndThen((isSuccess) => CombineSegments(task, outputStream))
                   .AndThen((isSuccess) => CleanUpTaskWithLogging(task, outputStream, "The download task has been completed successfully. " +
                        "Cleaning up the download progress and combining the segments."))
                   .OnFailure(errorCode => DownloadLogger.LogError($"PostDownloadProcessing failed: {errorCode}"));
        }

        /// <summary>
        /// Validate the state of the download task and the completed threads count to determine if the task has been cancelled or not and clean up the download progress if necessary.
        /// </summary>
        /// <param name="task">The download task to validate</param>
        /// <param name="outputStream">The output stream to clean up</param>
        /// <returns>The result of the validation and cleanup operation</returns>
        /// <remarks>
        /// This method is used to ensure that the download task is in a valid state before proceeding with the post-download processing
        /// and handle the situation that the task has been cancelled or not completed correctly.
        /// </remarks>
        private Result<bool, DownloadError> ValidateOrCleanup(DownloadTask task, Stream outputStream)
        {
            return (task.State, task.ThreadManager.CompletedThreadsCount == task.ThreadManager.MaxParallelThreads) switch
            {
                // If the state of the task is cancelled, it means that the download task has been cancelled
                // Clean up the download progress by closing and disposing the output stream and delete the file
                (DownloadState.Cancelled, _) => CleanUpTaskWithLogging(task, outputStream,
                    "Task has been cancelled. Cleaning up download progress."),

                // If the completed threads count is not equal to the max parallel threads count,
                // it means that the download task has not been completed but wrongly entered the PostDownloadProcessing method
                // In this case, we need to take care of the situation that the task has been cancelled
                // because the process has a major bug and cannot recover it easily.
                (_, false) => CleanUpTaskWithLogging(task, outputStream,
                    $"Task {task.ID} has not been completed but it enters wrongly to PostDownloadProcessing method. " +
                    $"Completed threads count: {task.ThreadManager.CompletedThreadsCount}, Max parallel threads: {task.ThreadManager.MaxParallelThreads}"),

                _ => Result<bool, DownloadError>.Success(true)
            };
        }

        /// <summary>
        /// Combine the segments of the file to a single file by using FileSegmentHelper.CombineSegmentsSafe method.
        /// </summary>
        /// <param name="task">The download task which contains the file segments to combine</param>
        /// <param name="outputStream">The final output stream to write the combined file to</param>
        /// <returns>The result of the operation</returns>
        private Result<bool, DownloadError> CombineSegments(DownloadTask task, Stream outputStream)
        {
            // Combine the segments of the file to a single file
            // This line is used to use ref keyword to pass the outputStream to the CombineSegmentsSafe method
            FileStream finalFileStream = outputStream as FileStream;
            Result<bool> result = FileSegmentHelper.CombineSegmentsSafe(
                task.ThreadManager.GetThreads().Select(x => x.FileSegmentPath).ToArray(),
                ref finalFileStream
            );
            if (!result.IsSuccess)
            {
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.UnexpectedOrUnknownException,
                    $"Failed to combine segments: {result.ErrorMessage}"));
            }
            return Result<bool, DownloadError>.Success(true);
        }
        #endregion

        #region Internal static methods for cleaning up the download progress

        /// <summary>
        /// Clean up the download progress by disposing the output stream and deleting the file segments with logging
        /// </summary>
        /// <param name="task"></param>
        /// <param name="outputStream"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        internal static Result<bool, DownloadError> CleanUpTaskWithLogging(DownloadTask task, Stream outputStream, string reason)
        {
            // Get the file segment paths from the task's thread manager
            string[] segmentPaths = task.ThreadManager.GetThreads().Select(x => x.FileSegmentPath).ToArray();

            // According to different reasons, log different levels of logs
            if (task.State == DownloadState.Cancelled)
            {
                DownloadLogger.LogInfo($"Task {task.ID} cleanup: {reason}");
                Debug.WriteLine($"Task {task.ID} has been cancelled. Cleaning up download progress.");
            }
            else
            {
                DownloadLogger.LogInfo($"Task {task.ID} cleanup: {reason}. " +
                    $"Completed threads: {task.ThreadManager.CompletedThreadsCount}, " +
                    $"Max threads: {task.ThreadManager.MaxParallelThreads}");
            }

            return CleanUpDownloadProcess(outputStream, segmentPaths);
        }

#nullable enable
        /// <summary>
        /// Clean up the download progress by closing and disposing the output stream
        /// </summary>
        /// <param name="targetStream">The output stream to clean up</param>
        /// <param name="filePaths">The file paths of the segments to delete</param>
        /// <returns>Whether the clean up is successful or not</returns>
        internal static Result<bool, DownloadError> CleanUpDownloadProcess(Stream targetStream, string[] filePaths)
        {
            // Clean up the download progress by closing and disposing the output stream
            // If the filePath is not null, delete the file
            // for why the filePath is null, it means that the download is not completed
            // e.g. The download only executed to DownloadFile().
            // Return a success result if the operation is successful.
            // Otherwise, return a failure result with the error code.
            return FileSegmentHelper.DeletSegements(filePaths).Match(
                // Match the result of deleting segments
                onSuccess: _ =>
                {
                    // Log the success message if necessary and clean up the target stream
                    DownloadLogger.LogInfo("Successfully deleted segments after download task/thread is cancelled or completed.");
                    // Clean up the target stream by closing and disposing it
                    return targetStream.CleanUp();
                },
                onFailure: errorCode =>
                {
                    // Log the error message if necessary
                    DownloadLogger.LogError($"When cleaning up the download progress for deleting segments, an error occurred: {errorCode}");
                    // If the deletion of segments failed, we still need to clean up the target stream to minimize the resource leak
                    return targetStream.CleanUp();
                }
            ).Match(
                // Match the result of cleaning up the target stream
                onSuccess: _ =>
                {
                    // Log the success message if necessary
                    DownloadLogger.LogInfo("Successfully cleaned up the output stream");
                    return Result<bool, DownloadError>.Success(true);
                },
                onFailure: errorCode =>
                {
                    // Return a failure result and log the error message at the same time
                    return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.UnexpectedOrUnknownException, 
                        $"When cleaning up the output stream, an error occurred: {errorCode}"));
                }
            );

        }
#nullable disable
        #endregion
    }
}