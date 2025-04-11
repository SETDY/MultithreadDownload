using MultithreadDownload.Downloads;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading;
using MultithreadDownload.Core;
using MultithreadDownload.Helpers;
using MultithreadDownload.Exceptions;

namespace MultithreadDownload.Protocols
{
    /// <summary>
    /// HttpDownloadService is a class that implements IDownloadService interface 
    /// which is for downloading file through http.
    /// </summary>
    public class HttpDownloadService : IDownloadService
    {
        /// <summary>
        /// Max retry times
        /// </summary>
        private const uint MAX_RETRY = 3;

        /// <summary>
        /// The wait time between retries in milliseconds
        /// </summary>
        private const int WAIT_TIME = 5000;

        public Result<Stream> GetStream(MultithreadDownload.Threading.DownloadThread downloadThread)
        {
            // Get the download context from the download thread.
            // Since the class is about Http, we can just cast it to HttpDownloadContext.
            HttpDownloadContext downloadContext = (HttpDownloadContext)downloadThread.DownloadContext;
            // Get a new httpClient instance.
            // Create a request message for a new httpClient instance.
            // Send a request and get the response.
            HttpClient client = HttpClientPool.GetClient();
            HttpRequestMessage requestMessage = CreateRequestMessage(
                downloadContext.Url,
                downloadContext.RangeStart,
                downloadContext.RangeOffset
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
                Debug.WriteLine($"Get a error message from the request: {result.ErrorMessage} which is from thread number {downloadThread.ID}");
                return Result<Stream>.Failure($"Thread number {downloadThread.ID} cannot connect to {downloadContext.Url}");
            }
            return Result<Stream>.Success(result.Value.Content.ReadAsStream());
        }

        private HttpRequestMessage CreateRequestMessage(string url, long downloadPosition, long downloadOffset)
        {
            // Set the method which is Get for the request and the offset of the requsted file.
            HttpRequestMessage httpRequestMessage =
                new HttpRequestMessage(HttpMethod.Get, url);
            httpRequestMessage.Headers.Range =
                new RangeHeaderValue(downloadPosition, downloadPosition + downloadOffset);
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

        public Result<int> DownloadFile(Stream input, Stream output, DownloadTaskThreadInfo threadInfo)
        {
            throw new NotImplementedException();
        }

        public Result<int> PostDownloadProcessing(Stream output, DownloadTaskThreadInfo threadInfo)
        {
            throw new NotImplementedException();
        }
    }
}
