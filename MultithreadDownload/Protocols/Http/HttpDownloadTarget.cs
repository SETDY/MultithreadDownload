using MultithreadDownload.Core.Errors;
using MultithreadDownload.Primitives;
using MultithreadDownload.Protocols.Abstractions;
using MultithreadDownload.Utils.Helpers;
using MultithreadDownload.Utils.IO;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace MultithreadDownload.Protocols.Http
{
    /// <summary>
    /// Represents the context for an HTTP download.
    /// </summary>
    public class HttpDownloadTarget : IDownloadTarget
    {

        /// <summary>
        /// The URL of the file to be downloaded.
        /// </summary>
        public Uri Url { get; set; }

        /// <summary>
        /// The file path where the downloaded file will be saved in.
        /// </summary>
        public FilePath FilePath { get; }

        /// <summary>
        /// The length of the content to be downloaded, in bytes. <br/>
        /// This value may be null if the content length is not known or cannot be determined.
        /// </summary>
        public long? ContentLength { get; }

        /// <summary>
        /// Whether the download target supports range requests. 
        /// </summary>
        /// <remarks>
        /// If true, the download can be performed in multiple threads by specifying byte ranges for each thread. <br/>
        /// If false, the download must be performed in a single thread without byte range specifications.
        /// </remarks>
        public bool SupportsRange => this.ContentLength is null;

        internal HttpDownloadTarget(FilePath filePath, Uri url, long? contentLength = null)
        {
            FilePath = filePath;
            Url = url;
            ContentLength = contentLength;
        }

        ///// <summary>
        ///// Gets the range size of the file for a thread to downloaded.
        ///// </summary>
        ///// <returns>The range size of the file in bytes.</returns>
        //public long GetRangeSize(byte threadID)
        //{
        //    // Get the range size for the specified thread
        //    // Get the start and end positions for the thread
        //    long startPosition = RangePositions[threadID, 0];
        //    long endPosition = RangePositions[threadID, 1];
        //    // Calculate and return the range size
        //    // If the start and end positions are both 0, return 0
        //    // Otherwise, return the range size
        //    return startPosition == 0 && endPosition == 0 
        //           ? 0 
        //           : endPosition - startPosition + 1;
        //}

        ///// <summary>
        ///// Creates a new instance of the HttpDownloadTarget class.
        ///// </summary>
        ///// <param name="maxParallelThreads">The maximum number of parallel threads for downloading.</param>
        ///// <param name="targetPath">The target path where the downloaded file will be saved.</param>
        ///// <param name="link">The URL of the file to be downloaded.</param>
        ///// <returns>The download context.</returns>
        ///// <remarks>
        ///// Since the file size is not known in advance, it can be zero or a exception can be thrown in the process of getting the file size.
        ///// Therefore, <see cref="GetDownloadContext"/> method returns a <see cref="Result{T, ErrorCode}"/> object
        ///// to indicate success or failure and remind the caller to check the result.
        ///// </remarks>
        //public static async Task<Result<HttpDownloadTarget, DownloadError>> Create(string url, string savedPath)
        //{

        //    //// Get the file size from the link
        //    //Result<long, DownloadError> fileSize = await HttpNetworkHelper.GetLinkFileSizeAsync(link);
        //    //// If the file size is not successfully retrieved, return result with error message
        //    //if (!fileSize.IsSuccess)
        //    //    return fileSize.Map<HttpDownloadTarget>(x => null);

        //    //// Otherwise, continue to create the download context
        //    //// Get the target path to save the downloaded file
        //    //string targetPath = GetDownloadSavedPath(link, savedPath);
        //    //// Since GetFileSegments() method requires a file size greater than 0,
        //    //// this if case is used to handle the case where the file size is 0
        //    //if (fileSize.ValueOrNull() == 0)
        //    //    return Result<HttpDownloadTarget, DownloadError>.Success(
        //    //        new HttpDownloadTarget(targetPath, maxParallelThreads, link, GetEmptyRanges(maxParallelThreads)));
        //    //// Otherwise, we can proceed to caculate the file segments for the download threads
        //    //// Get download size for each download thread
        //    //return FileSegmentHelper.CalculateFileSegmentRanges(fileSize.ValueOrNull(), maxParallelThreads)
        //    //    .AndThen(fileRange => Result<HttpDownloadTarget, DownloadError>.Success(
        //    //        new HttpDownloadTarget(targetPath, maxParallelThreads, link, rangePositions: fileRange)));
        //}

        ///// <summary>
        ///// Creates an empty range for each thread when the file size is 0.
        ///// </summary>
        ///// <param name="threadCount">The number of threads to create empty ranges for.</param>
        ///// <returns>The empty ranges for each thread.</returns>
        //private static long[,] GetEmptyRanges(byte threadCount)
        //{
        //    // FIXED: If the file size is 0, we cannot download the file with multiple threads.
        //    // Here we create an empty range for each thread
        //    // which means that each thread will not download any data.
        //    // Create an empty range for each thread
        //    long[,] emptyRanges = new long[threadCount, 2];
        //    for (int i = 0; i < threadCount; i++)
        //    {
        //        emptyRanges[i, 0] = 0;
        //        emptyRanges[i, 1] = 0;
        //    }
        //    return emptyRanges;
        //}
    }
}