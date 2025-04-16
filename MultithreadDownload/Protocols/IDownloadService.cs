using MultithreadDownload.Core;
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
        /// Get the stream of download file
        /// </summary>
        /// <param name="downloadContext">The context of the download.</param>
        /// <returns>The stream of the file to be downloaded.</returns>
        Result<Stream> GetStream(IDownloadContext downloadContext);

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