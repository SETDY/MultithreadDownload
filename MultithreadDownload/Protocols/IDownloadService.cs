using MultithreadDownload.Tasks;
using MultithreadDownload.Threading;
using MultithreadDownload.Threads;
using MultithreadDownload.Utils;
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
        Result<Stream[]> GetStreams(IDownloadContext downloadContext);

        /// <summary>
        /// Performs the actual download using the given input and output streams.
        /// </summary>
        /// <param name="inputStream">The input stream from which data is read.</param>
        /// <param name="outputStream">The output stream to which data is written.</param>
        /// <param name="threadInfo">Information about the current download thread.</param>
        /// <returns>A <see cref="Result{bool}"/> indicating whether the operation was successful, and the number of bytes written.</returns> 
        Result<int> DownloadFile(Stream inputStream, Stream outputStream, IDownloadThread downloadThread);

        /// <summary>
        /// Handles post-processing tasks after a all parts of file has been downloaded.
        /// </summary>
        /// <param name="output">The stream containing the downloaded data.</param>
        /// <param name="threadInfo">Information about the current download thread.</param>
        /// <returns>A <see cref="Result{bool}"/> indicating success or failure.</returns>
        Result<bool> PostDownloadProcessing(Stream outputStream, DownloadTask task);
    }
}