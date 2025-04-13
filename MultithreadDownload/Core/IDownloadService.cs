using MultithreadDownload.Tasks;
using System.IO;

namespace MultithreadDownload.Core
{
    /// <summary>
    /// Represents a download service implementation (e.g. HTTP, FTP).
    /// </summary>
    public interface IDownloadService
    {
        /// <summary>
        /// Prepares a stream for downloading a file part.
        /// </summary>
        /// <param name="downloadContext">Information about the current download.</param>
        /// <returns>A stream representing the target part of the file.</returns>
        Result<Stream> GetStream(IDownloadContext downloadContext);

        /// <summary>
        /// Performs the actual download using the given input and output streams.
        /// </summary>
        /// <param name="inputStream">The input stream from which data is read.</param>
        /// <param name="outputStream">The output stream to which data is written.</param>
        /// <param name="threadInfo">Information about the current download thread.</param>
        /// <returns>A <see cref="Result{T}"/> indicating whether the operation was successful, and the number of bytes written.</returns>
        Result<int> DownloadFile(Stream inputStream, Stream outputStream, DownloadThread threadInfo);

        /// <summary>
        /// Handles post-processing tasks after a all parts of file has been downloaded.
        /// </summary>
        /// <param name="output">The stream containing the downloaded data.</param>
        /// <param name="threadInfo">Information about the current download thread.</param>
        /// <returns>A <see cref="Result{T}"/> indicating success or failure.</returns>
        Result<int> PostDownloadProcessing(Stream output, DownloadThread);
    }
}