using MultithreadDownload.Core.Errors;
using MultithreadDownload.Core.Primitives;
using MultithreadDownload.Downloads;
using MultithreadDownload.Exceptions;
using MultithreadDownload.Logging;
using MultithreadDownload.Primitives;
using MultithreadDownload.Threads;
using MultithreadDownload.Threads.Abstractions;
using MultithreadDownload.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Protocols.Http
{
    internal sealed class HttpFileDownloader
    {
        #region Private Properties
        /// <summary>
        /// The set of options for configuring the HTTP download process, such as buffer size, maximum retries, and retry delay.
        /// </summary>
        private HttpDownloadOptions DownloadOptions { get; }

        /// <summary>
        /// The logger for logging the download process of the file
        /// </summary>
        private DownloadScopedLogger Logger { get; }

        /// <summary>
        /// The input stream from which data is read
        /// </summary>
        private Stream InputStream { get; }

        /// <summary>
        /// The output stream to which data is written
        /// </summary>
        private Stream OutputStream { get; }

        private CancellationToken AbortCancellationToken { get; }

        /// <summary>
        /// The number of bytes that are required to be downloaded for a chuck of file segment 
        /// that this download thread is responsible for downloading.
        /// </summary>
        private long RequiredDownloadBytesCount { get; set; }

        /// <summary>
        /// The buffer used for reading and writing data
        /// </summary>
        private byte[] DownloadBuffer { get; set; }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="FileDownloader"/> class.
        /// </summary>
        /// <param name="input">The input stream from which data is read</param>
        /// <param name="output">The output stream to which data is written</param>
        /// <param name="thread">The download thread that is responsible for downloading the file</param>
        public HttpFileDownloader(
            Stream input, 
            Stream output, 
            long bytesCount, 
            HttpDownloadOptions options,
            CancellationToken abortToken,
            DownloadScopedLogger logger)
        {
            // Initialize the properties
            this.InputStream = input;
            this.OutputStream = output;
            this.RequiredDownloadBytesCount = bytesCount;
            this.DownloadOptions = options;
            this.AbortCancellationToken = abortToken;
            this.Logger = logger;
            this.DownloadBuffer = new byte[DownloadOptions.BufferSize];
        }

        /// <summary>
        /// Download the file using the input and output streams
        /// </summary>
        public async Task<Result<long, DownloadError>> DownloadFileAsync()
        {
            // Log the start of the download process with the expected range size
            this.Logger.LogInfo($"Starting download for {this.RequiredDownloadBytesCount} bytes");
            // A variable is used to keep track of the actual number of bytes that have been downloaded for the file segment
            long actualDownloadedBytesCount = 0;
            // Since the download process may fail due to various exceptions,
            // use a try-catch block to catch any exceptions that may be thrown during the download process.
            try
            {
                // If the download process is not aborted or thrown any exception,
                // a while loop is used for keeping reading and writing data until the end of the stream is reached.
                while (true)
                {
                    // Check if the download process has been aborted before starting each read and write operation
                    this.AbortCancellationToken.ThrowIfCancellationRequested();

                    // Read a chunk of data from the input stream
                    // If the read operation failed, check if the maximum number of retries has been reached
                    // If is, this method will throw an exception to break the loop.
                    int bytesReadCount = (await this.TryReadChunkAsync()).UnwrapOrThrow();

                    // If the read operation was successful, check if the end of the stream has been reached
                    // If is, finish the download process
                    if (this.IsEndOfStream(bytesReadCount))
                        return this.FinishSuccessfully(actualDownloadedBytesCount);

                    // Write the chunk of data to the output stream
                    // If the write operation failed, check if the maximum number of retries has been reached
                    // If is, this method will throw an exception to break the loop.
                    (await this.TryWriteChunk(bytesReadCount)).UnwrapOrThrow();
                    // Update the download  by reporting the number of completed bytes to the download thread
                    this.UpdataDownloadProgress(bytesReadCount);
                    actualDownloadedBytesCount += bytesReadCount;
                }
            }
            // If any exception is thrown during the download process, create an error and finish the download process
            catch (ResultFailureException<DownloadError> ex)
            {
                return Result<long, DownloadError>.Failure(ex.Error);
            }
            catch (OperationCanceledException)
            {
                return Result<long, DownloadError>.Failure(DownloadError.Create(
                    DownloadErrorCode.ThreadAborted,
                    "The download process was aborted."));
            }
            catch (Exception ex)
            {
                return Result<long, DownloadError>.Failure(DownloadError.Create(
                    DownloadErrorCode.UnexpectedOrUnknownException,
                    $"An unexpected exception is thrown while downloading the file: {ex.InnerException?.Message}"));
            }
        }

        #region Private Methods
        /// <summary>
        /// Try to read a chunk of data from the input stream
        /// </summary>
        /// <param name="bytesRead">The number of bytes read</param>
        /// <param name="retryCount">The number of retries</param>
        /// <returns>The result of the operation</returns>
        private  async Task<Result<int, DownloadError>> TryReadChunkAsync()
        {
            // Check if the download process has been aborted before starting the read operation
            this.AbortCancellationToken.ThrowIfCancellationRequested();

            // Try to read a chunk of data from the input stream
            return await RetryHelper.RetryAsync(
                this.DownloadOptions.MaxRetries,
                TimeSpan.FromMicroseconds(this.DownloadOptions.BaseRetryDelayMs),
                TimeSpan.FromMicroseconds(this.DownloadOptions.MaxRetryDelayMs),
                () => this.ReadChunkAsync(),
                this.AbortCancellationToken);
            
        }

        private async Task<Result<int, DownloadError>> ReadChunkAsync()
        {
            // FIXED:
            // The problem is that the thread will download over the expected range size as there is no check for the remaining bytes to download in the while loop.
            // Therefore, use the GetRemainingBytesToDownload() method here to check if there are any bytes left to download
            // If there are no bytes left to download, set the bytesRead to 0 to indicate that the end of the stream has been reached.
            // Otherwise, read the data from the input stream into the buffer.
            if (this.RequiredDownloadBytesCount == 0)
            {
                // return 0 to indicate that the end of the stream has been reached
                return Result<int,DownloadError>.Success(0);
            }

            // FIXED:
            // The problem is that the buffer size is not adjusted according to the remaining bytes to download when there are less bytes left to download than the buffer size.
            // Therefore, check if the buffer size is greater than the remaining bytes to download
            // If so, resize the buffer to the remaining bytes to download
            // This will ensure that the buffer size is always equal to or less than the remaining bytes to download, to prevent reading more data than needed.
            // Otherwise, keep the buffer size as it is.
            if (this.DownloadBuffer.Length > this.RequiredDownloadBytesCount)
            {
                this.DownloadBuffer = new byte[this.RequiredDownloadBytesCount];
            }

            // Read the data from the input stream into the buffer and return the number of bytes read
            try
            {
                return Result<int, DownloadError>.Success(await this.InputStream.ReadAsync(
                    this.DownloadBuffer, 
                    0, 
                    this.DownloadBuffer.Length, 
                    this.AbortCancellationToken));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Result<int, DownloadError>.Failure(DownloadError.Create(
                    DownloadErrorCode.ArgumentOutOfRange,
                    $"An argument out of range error occurred while reading a chunk of data: {ex.Message}"));
            }
            catch (OperationCanceledException ex)
            {
                return Result<int, DownloadError>.Failure(DownloadError.Create(
                    DownloadErrorCode.ThreadAborted,
                    $"The download process was aborted while reading a chunk of data: {ex.Message}"));
            }
            catch(Exception ex)
            {
                return Result<int, DownloadError>.Failure(DownloadError.Create(
                    DownloadErrorCode.UnexpectedOrUnknownException,
                    $"An unexpected exception is thrown while reading a chunk of data: {ex.Message}"));
            }
        }

        /// <summary>
        /// Try to write a chunk of data to the output stream
        /// </summary>
        /// <param name="bytesRead">The number of bytes read</param>
        /// <param name="retryCount"><The number of retries</param>
        /// <returns>The result of the operation</returns>
        private async Task<Result<None, DownloadError>> TryWriteChunk(int bytesCount)
        {
            // Try to write a chunk of data from the input stream
            return await RetryHelper.RetryAsync(
                this.DownloadOptions.MaxRetries,
                TimeSpan.FromMicroseconds(this.DownloadOptions.BaseRetryDelayMs),
                TimeSpan.FromMicroseconds(this.DownloadOptions.MaxRetryDelayMs),
                () => this.WriteChunkAsync(bytesCount),
                this.AbortCancellationToken);
        }

        private async Task<Result<None, DownloadError>> WriteChunkAsync(int bytesCount)
        {

            // Write the data from the input stream into the output stream
            try
            {
                await this.OutputStream.WriteAsync(this.DownloadBuffer, 0, bytesCount, this.AbortCancellationToken);
                return Result<None, DownloadError>.Success(None.Create());
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Result<None, DownloadError>.Failure(DownloadError.Create(
                    DownloadErrorCode.ArgumentOutOfRange,
                    $"An argument out of range error occurred while reading a chunk of data: {ex.Message}"));
            }
            catch (OperationCanceledException ex)
            {
                return Result<None, DownloadError>.Failure(DownloadError.Create(
                    DownloadErrorCode.ThreadAborted,
                    $"The download process was aborted while reading a chunk of data: {ex.Message}"));
            }
            catch (Exception ex)
            {
                return Result<None, DownloadError>.Failure(DownloadError.Create(
                    DownloadErrorCode.UnexpectedOrUnknownException,
                    $"An unexpected exception is thrown while reading a chunk of data: {ex.Message}"));
            }
        }

        /// <summary>
        /// Check if the end of the stream has been reached
        /// </summary>
        /// <param name="bytesRead">
        /// The number of bytes read
        /// </param>
        /// <returns>
        /// Whether the end of the stream is reached
        /// </returns>
        private bool IsEndOfStream(int bytesRead)
        {
            return bytesRead == 0;
        }

        /// <summary>
        /// Update the download progress by adding the number of bytes that have been downloaded to the required download bytes count
        /// </summary>
        /// <param name="bytesCount">
        /// The number of new bytes that have been downloaded, which will be added to the required download bytes count to update the download progress.
        /// </param>
        private void UpdataDownloadProgress(int bytesCount)
        {
            this.RequiredDownloadBytesCount += bytesCount;
        }

        /// <summary>
        /// Finish the download process successfully
        /// </summary>
        /// <param name="output">The output stream</param>
        /// <returns></returns>
        private Result<long, DownloadError> FinishSuccessfully(long actualDownloadedBytesCount)
        {
            // Ensure that the download process is completed successfully
            EnsureDownloadCompleted();

            // Log the successful completion of the download process
            this.Logger.LogInfo($"A chuck of the file segment has been downloaded successfully.");
            // Return the result of the operation
            // Since the download process is completed successfully, return the actual number of bytes downloaded
            return Result<long, DownloadError>.Success(actualDownloadedBytesCount);
        }

        /// <summary>
        /// Ensure that the progress is reasonable
        /// </summary>
        private void EnsureDownloadCompleted()
        {
            if (this.RequiredDownloadBytesCount != 0)
            {
                throw new UnreachableException("The download process is completed but the required download bytes count is not zero, which is unexpected.");
            }
        }

        ///// <summary>
        ///// Set the number of completed bytes for the download thread
        ///// </summary>
        ///// <param name="count">The number of completed bytes</param>
        //private bool SetCompletedByteNumbers(int count)
        //{
        //    // Add the completed bytes size count to the download thread
        //    _thread.AddCompletedBytesSizeCount(count);
        //    try
        //    {
        //        // Update the progress of the download thread
        //        UpdateThreadProgress();
        //        return true;
        //    }
        //    // If updating the progress fails, log the error and return false
        //    catch (InvalidOperationException ex)
        //    {
        //        // Log the failure and the exception when trying to update the download progress
        //        this._thread.Logger.LogError("Failed to updating the download progress of the thread", ex);
        //        return false;
        //    }
        //}

        /// <summary>
        /// Update the progress of the download thread
        /// </summary>
        //private void UpdateThreadProgress()
        //{
        //    // Get the download context of the thread
        //    // Get the size that the thread should be downloaded (rangeSize)
        //    HttpDownloadTarget downloadContext = (HttpDownloadTarget)_thread.DownloadContext;
        //    long rangeSize = downloadContext.GetRangeSize(_thread.ID);

        //    // Check if the completed bytes size count is greater than the range size
        //    if (_thread.CompletedBytesSizeCount > rangeSize)
        //    {
        //        // If the completed bytes size count is greater than the range size, log an error and throw an exception
        //        this._thread.Logger.LogError($"Thread has completed bytes size count ({_thread.CompletedBytesSizeCount}) " +
        //            $"which is unexpectly greater than the range size ({rangeSize}), causing the download is failed.");
        //        throw new InvalidOperationException($"Thread with ID {_thread.ID} has completed bytes size count ({_thread.CompletedBytesSizeCount}) greater than the range size ({rangeSize}).");
        //    }    

        //    // Calculate the progress of the thread
        //    // If the rangeSize is zero that means that the thread does not need to download any thing,
        //    // the download progress should be set to 100%
        //    // Otherwise, the progress should be calculated by the completed bytes size count
        //    sbyte progress = rangeSize > 0
        //        ? (sbyte)(_thread.CompletedBytesSizeCount / (decimal)rangeSize * 100)
        //        : (sbyte)100;

        //    // Set the progress of the thread if the thread state is not completed
        //    if (_thread.State != DownloadState.Completed)
        //        _thread.SetDownloadProgress(progress);
        //}
        #endregion
    }
}