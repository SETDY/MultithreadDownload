using MultithreadDownload.Downloads;
using MultithreadDownload.Logging;
using MultithreadDownload.Threads;
using MultithreadDownload.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MultithreadDownload.Core.Errors;

namespace MultithreadDownload.Protocols.Http
{
    internal class HttpFileDownloader
    {
        #region Private Fields
        /// <summary>
        /// The size of the buffer used for reading and writing data
        /// </summary>
        private const int BUFFER_SIZE = 4096;

        /// <summary>
        /// The maximum number of retries for reading and writing data
        /// </summary>
        private const int MAX_TOTAL_RETRIES = 5;

        /// <summary>
        /// The time to wait before retrying to read or write data
        /// </summary>
        private const int WAIT_MS = 2000;

        /// <summary>
        /// The download thread that is responsible for downloading the file
        /// </summary>
        private readonly IDownloadThread _thread;

        /// <summary>
        /// The input stream from which data is read
        /// </summary>
        private readonly Stream _input;

        /// <summary>
        /// The output stream to which data is written
        /// </summary>
        private readonly Stream _output;

        /// <summary>
        /// The buffer used for reading and writing data
        /// </summary>
        private readonly byte[] _buffer = new byte[BUFFER_SIZE];
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="FileDownloader"/> class.
        /// </summary>
        /// <param name="input">The input stream from which data is read</param>
        /// <param name="output">The output stream to which data is written</param>
        /// <param name="thread">The download thread that is responsible for downloading the file</param>
        public HttpFileDownloader(Stream input, Stream output, IDownloadThread thread)
        {
            _input = input;
            _output = output;
            _thread = thread;
        }

        /// <summary>
        /// Download the file using the input and output streams
        /// </summary>
        /// <returns>Whether the download was successful or not</returns>
        public Result<bool, DownloadError> DownloadFile()
        {
            // Set the state of the download thread to downloading
            _thread.SetState(DownloadState.Downloading);
            // Initialize the retry count to 0
            // The retry count is used to keep track of the number of retries for reading and writing data in same process loop
            // Therefore, if the write operation fails and then the read operation fails, the retry count will be incremented by 2 totally
            int retryCount = 0;
            DownloadError failedError = null;

            // If the state of the download thread is downloading, continue the download process
            while (_thread.State == DownloadState.Downloading)
            {
                // Read a chunk of data from the input stream
                // If the read operation failed, check if the maximum number of retries has been reached
                // If is, break the loop.
                if (!TryReadChunk(out int bytesRead, ref retryCount))
                {
                    // Set the failed error to indicate that the read operation failed after maximum retries
                    failedError = DownloadError.Create(DownloadErrorCode.HttpError, "Failed to read data from input stream after maximum retries.");
                    break;
                }
                //Debug.WriteLine("Completed Bytes Size Count: " + _thread.CompletedBytesSizeCount);
                // Since the read operation was successful, reset the retry count
                retryCount = 0;
                // If the read operation was successful, check if the end of the stream has been reached
                // If is, finish the download process
                if (IsEndOfStream(bytesRead))
                    return FinishDownload();
                // Write the chunk of data to the output stream
                // If the write operation failed, check if the maximum number of retries has been reached
                // If is, break the loop.
                // Add the number of completed bytes for the download thread
                if (!TryWriteChunk(bytesRead, ref retryCount))
                {
                    // Set the failed error to indicate that the write operation failed after maximum retries
                    failedError = DownloadError.Create(DownloadErrorCode.DiskOperationFailed, "Failed to write data to output stream after maximum retries.");
                    break;
                }
                // Log the number of completed bytes for the download thread if needed
                //DownloadLogger.LogInfo($"Set Download Bytes from {_thread.CompletedBytesSizeCount} to {_thread.CompletedBytesSizeCount + bytesRead} at {DateTime.Now}");
                SetCompletedByteNumbers(bytesRead);
            }

            // FIXED: This is used to fix when the download thread state is completed when the last chunk is written to the output stream,
            //        but it does not be checked in the while loop by IsEndOfStream(), so the download process is not finished successfully.
            // If the download thread state is not downloading, it means that the download process has been completed or failed
            // Check if the failed error is null and the download thread state is completed, which means that the download process was successful
            // If so, finish the download process successfully
            // Otherwise, finish the download process with the failed error
            if (failedError == null && _thread.State == DownloadState.Completed)
                return FinishDownload();
            return FinishFailed(failedError);
        }

        #region Private Methods
        /// <summary>
        /// Try to read a chunk of data from the input stream
        /// </summary>
        /// <param name="bytesRead">The number of bytes read</param>
        /// <param name="retryCount">The number of retries</param>
        /// <returns>The result of the operation</returns>
        private bool TryReadChunk(out int bytesRead, ref int retryCount)
        {
            try
            {
                //Debug.WriteLine("Try to read chunk to input stream");
                bytesRead = _input.Read(_buffer, 0, _buffer.Length);
                return true;
            }
            catch
            {
                //Debug.WriteLine("Failed to read chunk from input stream, retrying...");
                bytesRead = 0;
                return HandleRetry(ref retryCount);
            }
        }

        /// <summary>
        /// Try to write a chunk of data to the output stream
        /// </summary>
        /// <param name="bytesRead">The number of bytes read</param>
        /// <param name="retryCount"><The number of retries</param>
        /// <returns>The result of the operation</returns>
        private bool TryWriteChunk(int bytesRead, ref int retryCount)
        {
            try
            {
                _output.Write(_buffer, 0, bytesRead);
                return true;
            }
            catch
            {
                return HandleRetry(ref retryCount);
            }
        }

        /// <summary>
        /// Handle the retry logic for reading and writing data
        /// </summary>
        /// <param name="retryCount">The number of retries</param>
        /// <returns>The result of the operation</returns>
        private bool HandleRetry(ref int retryCount)
        {
            // If the maximum number of retries has been reached, return false
            // Otherwise, wait for a specified time and return true to retry the operation
            if (++retryCount >= MAX_TOTAL_RETRIES)
                return false;
            Thread.Sleep(WAIT_MS);
            return true;
        }

        /// <summary>
        /// Check if the end of the stream has been reached
        /// </summary>
        /// <param name="bytesRead">The number of bytes read</param>
        /// <returns>Is the end of the stream reached</returns>
        private bool IsEndOfStream(int bytesRead)
        {
            return bytesRead == 0;
        }

        /// <summary>
        /// Finish the download process successfully
        /// </summary>
        /// <returns>The result of the operation</returns>
        private Result<bool, DownloadError> FinishDownload()
        {
            // Since if the file is empty, the completed bytes size count will be zero,
            // we need to make sure the progress is set when the file is empty
            if (_thread.CompletedBytesSizeCount == 0)
                EnsureNonZeroProgress();

            return FinishSuccessfully(_output);
        }

        /// <summary>
        /// Ensure that the progress is non-zero to set the completed bytes size count to the expected value
        /// </summary>
        private void EnsureNonZeroProgress()
        {
            long[,] downloadedRange = ((HttpDownloadContext)_thread.DownloadContext).RangePositions;
            long expected = downloadedRange[_thread.ID, 1] - downloadedRange[_thread.ID, 0];
            // Set the completed bytes size count to the expected value
            SetCompletedByteNumbers((int)expected);
        }

        /// <summary>
        /// Set the number of completed bytes for the download thread
        /// </summary>
        /// <param name="count">The number of completed bytes</param>
        private void SetCompletedByteNumbers(int count)
        {
            _thread.AddCompletedBytesSizeCount(count);
            UpdateThreadProgress();
        }

        /// <summary>
        /// Update the progress of the download thread
        /// </summary>
        private void UpdateThreadProgress()
        {
            // Get the download context of the thread
            // Caculate the size that the thread should be downloaded (rangeSize)
            HttpDownloadContext downloadContext = (HttpDownloadContext)_thread.DownloadContext;
            long rangeSize = downloadContext.RangePositions[_thread.ID, 1] -
                downloadContext.RangePositions[_thread.ID, 0];

            // Calculate the progress of the thread
            // If the rangeSize is zero that means that the thread does not need to download any thing,
            // the download progress should be set to 100%
            // Otherwise, the progress should be calculated by the completed bytes size count
            sbyte progress = rangeSize > 0
                ? (sbyte)(_thread.CompletedBytesSizeCount / (decimal)rangeSize * 100)
                : (sbyte)100;

            // Set the progress of the thread if the thread state is not completed
            if (_thread.State != DownloadState.Completed)
                _thread.SetDownloadProgress(progress);
        }

        /// <summary>
        /// Finish the download process successfully
        /// </summary>
        /// <param name="output">The output stream</param>
        /// <returns></returns>
        private Result<bool, DownloadError> FinishSuccessfully(Stream output)
        {
            // Clean up the download progress by closing and disposing the output stream
            // This will also ensure that the output stream is flushed and all data is written
            DownloadLogger.LogInfo($"The thread with ID {_thread.ID} completed successfully: {((HttpDownloadContext)_thread.DownloadContext).Url}");
            HttpDownloadService.CleanUpDownloadProcess(output, Array.Empty<string>());
            return Result<bool, DownloadError>.Success(true);
        }

        /// <summary>
        /// Finish the download process failed
        /// </summary>
        /// <param name="output">The output stream</param>
        /// <param name="context">The download context</param>
        /// <param name="retries">The number of retryment</param>
        /// <returns>The result of the operation</returns>
        private Result<bool, DownloadError> FinishFailed(DownloadError error)
        {
            // Clean up the download progress by closing and disposing the output stream
            DownloadLogger.LogError($"Thread failed after {MAX_TOTAL_RETRIES} retries: {((HttpDownloadContext)_thread.DownloadContext).Url}");
            HttpDownloadService.CleanUpDownloadProcess(_output, new string[] { _thread.FileSegmentPath });
            return Result<bool, DownloadError>.Failure(error);
        }
        #endregion
    }
}