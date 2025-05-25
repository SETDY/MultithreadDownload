using MultithreadDownload.Downloads;
using MultithreadDownload.Logging;
using MultithreadDownload.Threads;
using MultithreadDownload.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Protocols.Http
{
    internal class HttpFileDownloader
    {
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
        public Result<bool> DownloadFile()
        {
            _thread.SetState(DownloadState.Downloading);
            int retryCount = 0;

            while (_thread.State == DownloadState.Downloading)
            {
                // Read a chunk of data from the input stream
                // If the read operation failed, check if the maximum number of retries has been reached
                // If is, break the loop.
                if (!TryReadChunk(out int bytesRead, ref retryCount))
                    break;
                // Reset the retry count to 0
                retryCount = 0;
                // If the read operation was successful, check if the end of the stream has been reached
                // If is, finish the download process
                if (IsEndOfStream(bytesRead))
                    return FinishDownload();
                // Set the number of completed bytes for the download thread
                // Write the chunk of data to the output stream
                // If the write operation failed, check if the maximum number of retries has been reached
                // If is, break the loop.
                //DownloadLogger.LogInfo($"Set Download Bytes from {_thread.CompletedBytesSizeCount} to {_thread.CompletedBytesSizeCount + bytesRead} at {DateTime.Now}");
                SetCompletedByteNumbers(bytesRead);
                if (!TryWriteChunk(bytesRead, ref retryCount))
                    break;
            }

            return FinishFailed();
        }

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
                bytesRead = _input.Read(_buffer, 0, _buffer.Length);
                return true;
            }
            catch
            {
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
        private Result<bool> FinishDownload()
        {
            // Make sure the progress is set when the file is empty
            if (_thread.CompletedBytesSizeCount == 0)
                EnsureNonZeroProgress();

            return FinishSuccessfully(_output);
        }

        /// <summary>
        /// Ensure that the progress is non-zero
        /// </summary>
        private void EnsureNonZeroProgress()
        {
            var range = ((HttpDownloadContext)_thread.DownloadContext).RangePositions;
            long expected = range[_thread.ID, 1] - range[_thread.ID, 0];
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

            // Set the progress of the thread
            if (_thread.State != DownloadState.Completed)
                _thread.SetDownloadProgress(progress);
        }

        /// <summary>
        /// Finish the download process successfully
        /// </summary>
        /// <param name="output">The output stream</param>
        /// <returns></returns>
        private Result<bool> FinishSuccessfully(Stream output)
        {
            CleanUpDownloadProgess(output, null);
            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Finish the download process failed
        /// </summary>
        /// <param name="output">The output stream</param>
        /// <param name="context">The download context</param>
        /// <param name="retries">The number of retryment</param>
        /// <returns>The result of the operation</returns>
        private Result<bool> FinishFailed()
        {
            CleanUpDownloadProgess(_output, null);
            return Result<bool>.Failure(
                $"Thread failed after {MAX_TOTAL_RETRIES} retries: {((HttpDownloadContext)_thread.DownloadContext).Url}");
        }

        /// <summary>
        /// Clean up the download progress by closing and disposing the output stream
        /// </summary>
        /// <param name="targetStream">The output stream</param>
        /// <param name="filePaths">The file paths to delete</param>
        /// <returns>The result of the operation</returns>
        private Result<bool> CleanUpDownloadProgess(Stream targetStream, string[] filePaths)
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