using MultithreadDownload.Core.Errors;
using MultithreadDownload.Logging;
using MultithreadDownload.Primitives;
using MultithreadDownload.Tasks;
using MultithreadDownload.Threads;
using System.IO;

namespace MultithreadDownload.Protocols
{
    /// <summary>
    /// Represents a download service implementation (e.g. HTTP, FTP).
    /// </summary>
    public interface IDownloadService
    {
        /// <summary>
        /// Get the streams for each of the download threads of the download task.
        /// </summary>
        /// <param name="downloadContext"></param>
        /// <param name="rangePostions"></param>
        /// <returns>The streams for each of the download threads of the download task</returns>
        Result<Stream[], DownloadError> GetStreams(IDownloadContext downloadContext, DownloadScopedLogger logger);

        /// <summary>
        /// Performs the actual download using the given input and output streams.
        /// </summary>
        /// <param name="inputStream">The input stream from which data is read.</param>
        /// <param name="outputStream">The output stream to which data is written.</param>
        /// <param name="threadInfo">Information about the current download thread.</param>
        /// <returns>A <see cref="Result{bool,DownloadError}"/> indicating whether the operation was successful.</returns>
        Result<bool, DownloadError> DownloadFile(Stream inputStream, Stream outputStream, IDownloadThread downloadThread);

        /// <summary>
        /// Handles post-processing tasks after a all parts of file has been downloaded.
        /// </summary>
        /// <param name="output">The stream containing the downloaded data.</param>
        /// <param name="threadInfo">Information about the current download thread.</param>
        /// <returns>A <see cref="Result{bool,DownloadError}"/> indicating success or failure.</returns>
        Result<bool, DownloadError> PostDownloadProcessing(Stream outputStream, DownloadTask task);

        /// <summary>
        /// Determines whether the specified download context is supported by the implementation.
        /// </summary>
        /// <param name="downloadContext">The download context to evaluate for support. Cannot be null.</param>
        /// <returns>true if the specified download context is supported; otherwise, false.</returns>
        bool IsSupportedDownloadContext(IDownloadContext downloadContext);
    }
}