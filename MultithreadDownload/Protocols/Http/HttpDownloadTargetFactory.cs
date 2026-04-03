using MultithreadDownload.Core.Errors;
using MultithreadDownload.Primitives;
using MultithreadDownload.Utils.Helpers;
using MultithreadDownload.Utils.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Protocols.Http
{
    public static class HttpDownloadTargetFactory
    {
        /// <summary>
        /// Creates a new instance of the HttpDownloadTarget class.
        /// </summary>
        /// <param name="maxParallelThreads">The maximum number of parallel threads for downloading.</param>
        /// <param name="targetPath">The target path where the downloaded file will be saved.</param>
        /// <param name="link">The URL of the file to be downloaded.</param>
        /// <returns>The download context.</returns>
        /// <remarks>
        /// Since the file size is not known in advance, it can be zero or a exception can be thrown in the process of getting the file size.
        /// Therefore, <see cref="GetDownloadContext"/> method returns a <see cref="Result{T, ErrorCode}"/> object
        /// to indicate success or failure and remind the caller to check the result.
        /// </remarks>
        public static async Task<Result<HttpDownloadTarget, DownloadError>> Create(string url, string savedPath)
        {
            return await Create(url, savedPath, new IncrementalFileNameConflictResolver());
        }

        /// <summary>
        /// Creates a new instance of the HttpDownloadTarget class.
        /// </summary>
        /// <param name="maxParallelThreads">The maximum number of parallel threads for downloading.</param>
        /// <param name="targetPath">The target path where the downloaded file will be saved.</param>
        /// <param name="link">The URL of the file to be downloaded.</param>
        /// <returns>The download context.</returns>
        /// <remarks>
        /// Since the file size is not known in advance, it can be zero or a exception can be thrown in the process of getting the file size.
        /// Therefore, <see cref="GetDownloadContext"/> method returns a <see cref="Result{T, ErrorCode}"/> object
        /// to indicate success or failure and remind the caller to check the result.
        /// </remarks>
        public static async Task<Result<HttpDownloadTarget, DownloadError>> Create(
            string url, 
            string savedPath, 
            IFileNameConflictResolver conflictResolver)
        {
            // Chain the operations to create the download context using the Result type to handle errors
            // Check if the paraments are valid
            // If the paraments are not valid, return result with error message
            // Otherwise, continue to get the file size and create the download context
            return await (await IsParamentsVaild(url, savedPath)).
                AndThenAnsyc(async x => (await HttpNetworkHelper.GetLinkFileSizeAsync(url))
                    .Map(fileSize => new HttpDownloadTarget(
                        GetDownloadFilePath(url, savedPath, conflictResolver),
                        new Uri(url),
                        // If the file size is 0, it means the file size is not known, so we set it to null
                        fileSize == 0 ? null : fileSize)));
        }

        /// <summary>
        /// Checks if the paraments provided for a download target are valid.
        /// </summary>
        /// <returns>The result indicating whether the properties are valid or not.</returns>
        private static async Task<Result<bool, DownloadError>> IsParamentsVaild(string url, string savedPath)
        {
            // Check if the target path is valid
            if (string.IsNullOrEmpty(savedPath))
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.PathNotFound, "Saved path is not valid."));
            // Check if the URL is valid
            if (string.IsNullOrEmpty(url) || ! await HttpNetworkHelper.LinkCanConnectionAsync(url))
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.InvalidUrl, "Url is not valid."));
            return Result<bool, DownloadError>.Success(true);
        }

        /// <summary>
        /// Gets the file path to be used for the download.
        /// </summary>
        /// <param name="link">The URL of the file to be downloaded.</param>
        /// <param name="savedPath">The path where the downloaded file will be saved.</param>
        /// <returns>The file path to be used for the download.</returns>
        private static FilePath GetDownloadFilePath(string url, string savedPath, IFileNameConflictResolver conflictResolver)
        {
            // Get and return the FilePath instance for a unique file name
            return new FilePath(
                    PathHelper.GetDirectoryNameSafe(savedPath),
                    GetDownloadFileName(url, savedPath))
                .EnsureUnique(conflictResolver);
        }

        /// <summary>
        /// Gets the download file name from the link and saved path.
        /// </summary>
        /// <param name="link">The URL of the file to be downloaded.</param>
        /// <param name="savedPath">The path where the downloaded file will be saved.</param>
        /// <returns>The file name to be used for the download.</returns>
        private static string GetDownloadFileName(string url, string savedPath)
        {
            //FIXED: There is a fix for the issue of empty file name
            // If the file name is not specified, use the file name from the link
            string targetFileName = Path.GetFileName(savedPath);
            if (string.IsNullOrEmpty(targetFileName))
                targetFileName = Path.GetFileName(url);
            // If the file name is also not specified, use a random file name
            if (string.IsNullOrEmpty(targetFileName))
                targetFileName = Path.GetRandomFileName();
            return targetFileName;
        }
    }
}
