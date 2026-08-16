using MultithreadDownload.Core.Errors;
using MultithreadDownload.Core.Primitives;
using MultithreadDownload.Logging;
using MultithreadDownload.Primitives;
using MultithreadDownload.Tasks.Core;
using MultithreadDownload.Threads.Abstractions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Protocols.Abstractions
{
    /// <summary>
    /// Represents a download service implementation (e.g. HTTP, FTP).
    /// </summary>
    public interface IDownloadService<TTarget> where TTarget : IDownloadTarget
    {
        /// <summary>
        /// Get the stream for the download threads of the download task.
        /// </summary>
        /// <param name="downloadContext"></param>
        /// <param name="rangePostions"></param>
        /// <returns>The streams for each of the download threads of the download task</returns>
        public Task<Result<Stream, DownloadError>> GetChunkStreamAsync(
            TTarget downloadContext, 
            long startPosition,
            long endPosition,
            CancellationToken cancellationToken,
            DownloadScopedLogger logger);


        public Task<Result<long, DownloadError>> DownloadChunkAsync(
            Stream inputDownloadingStream,
            Stream outputWritingStream,
            long bytesCount,
            CancellationToken cancellationToken,
            DownloadScopedLogger logger);

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
        bool IsSupportedDownloadContext(IDownloadTarget downloadContext);
    }
}