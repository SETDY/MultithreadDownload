using FluentAssertions;
using MultithreadDownload.Core;
using MultithreadDownload.Protocols.Http;
using MultithreadDownload.Tasks;
using MultithreadDownload.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MultithreadDownload.Core.Errors;

namespace MultithreadDownload.IntegrationTests.Fixtures
{
    /// <summary>
    /// TestHelper provides utility methods for setting up tests, preparing downloads, and verifying file content.
    /// </summary>
    public static class TestHelper
    {
        #region Http Download Helper methods
        /// <summary>
        /// Prepares the download by starting a local HTTP server and creating a download manager.
        /// </summary>
        /// <param name="downloadServiceType">The type of download service to use.</param>
        /// <param name="maxThreads">The maximum number of threads to use for downloading.</param>
        /// <param name="url">The link to the file to be downloaded.</param>
        /// <param name="outputPath">The path where the downloaded file will be saved.</param>
        /// <param name="realFilePath">The path to the real file on the server.</param>
        /// <returns></returns>
        public static async Task<(LocalHttpFileServer Server, MultiDownload Manager, HttpDownloadContext? Context)>
        PrepareFullHttpDownloadEnvironment(DownloadServiceType downloadServiceType ,byte maxParallelTasks, byte maxDownloadThreads, string outputPath, string realFilePath)
        {
            return await PrepareFullHttpDownloadEnvironment(downloadServiceType, maxParallelTasks, maxDownloadThreads, outputPath, File.ReadAllBytes(realFilePath));
        }

        /// <summary>
        /// Prepares the download by starting a local HTTP server and creating a download manager.
        /// </summary>
        /// <param name="downloadServiceType">The type of download service to use.</param>
        /// <param name="maxThreads">The maximum number of threads to use for downloading.</param>
        /// <param name="url">The link to the file to be downloaded.</param>
        /// <param name="outputPath">The path where the downloaded file will be saved.</param>
        /// <param name="testData">The byte array containing the test data to be served by the local HTTP server.</param>
        /// <returns></returns>
        public static async Task<(LocalHttpFileServer Server, MultiDownload Manager, HttpDownloadContext? Context)>
        PrepareFullHttpDownloadEnvironment(DownloadServiceType downloadServiceType, byte maxParallelTasks, byte maxDownloadThreads, string outputPath, byte[] testData)
        {
            (var server, var manager, var url) = PreparePartialHttpDownloadEnvironment(downloadServiceType, maxParallelTasks, testData);

            return (server, manager, await GetHttpDownloadContext(maxDownloadThreads, url, outputPath));
        }

        /// <summary>
        /// Prepares the download by starting a local HTTP server and creating a download manager.
        /// </summary>
        /// <param name="downloadServiceType">The type of download service to use.</param>
        /// <param name="maxThreads">The maximum number of threads to use for downloading.</param>
        /// <param name="url">The link to the file to be downloaded.</param>
        /// <param name="outputPath">The path where the downloaded file will be saved.</param>
        /// <param name="realFilePath">The path to the real file on the server.</param>
        /// <returns>The local HTTP server, the download manager, and the URL of the file to be downloaded.</returns>
        public static (LocalHttpFileServer Server, MultiDownload Manager, string url)
        PreparePartialHttpDownloadEnvironment(DownloadServiceType downloadServiceType, byte maxParallelTasks, string realFilePath)
        {
            return PreparePartialHttpDownloadEnvironment(downloadServiceType, maxParallelTasks, File.ReadAllBytes(realFilePath));
        }

        /// <summary>
        /// Prepares the download by starting a local HTTP server and creating a download manager.
        /// </summary>
        /// <param name="downloadServiceType">The type of download service to use.</param>
        /// <param name="maxThreads">The maximum number of threads to use for downloading.</param>
        /// <param name="url">The link to the file to be downloaded.</param>
        /// <param name="outputPath">The path where the downloaded file will be saved.</param>
        /// <param name="realFilePath">The path to the real file on the server.</param>
        /// <param name="testData">The byte array containing the test data to be served by the local HTTP server.</param>
        public static  (LocalHttpFileServer Server, MultiDownload Manager, string url)
        PreparePartialHttpDownloadEnvironment(DownloadServiceType downloadServiceType, byte maxParallelTasks, byte[] testData)
        {
            // Create a local HTTP server with the provided test data
            LocalHttpFileServer server = new LocalHttpFileServer(testData);
            // Create and start the server to serve the test data
            server.Create();
            server.Start();

            MultiDownload downloadManager = new MultiDownload(maxParallelTasks, downloadServiceType);

            return (server, downloadManager, server.Url);
        }

        /// <summary>
        /// Gets the HTTP download context for a given URL and output path.
        /// </summary>
        /// <param name="maxDownloadThreads">The maximum number of threads to use for downloading.</param>
        /// <param name="url">The link to the file to be downloaded.</param>
        /// <param name="outputPath">The path where the downloaded file will be saved.</param>
        /// <returns>The HTTP download context containing information about the download task.</returns>
        public static async Task<HttpDownloadContext> GetHttpDownloadContext(byte maxDownloadThreads,string url, string outputPath)
        {
            Result<HttpDownloadContext, DownloadError> contextResult =
                await HttpDownloadContext.GetDownloadContext(maxDownloadThreads, outputPath, url);
            contextResult.Should().NotBeNull();
            contextResult.IsSuccess.Should().BeTrue();
            contextResult.Value.Should().NotBeNull();
            return contextResult.Value.Value;
        }
        #endregion

        #region Test Helper Methods

        /// <summary>
        /// Checks if the test is running on a Continuous Integration (CI) environment and skips the test if it is.
        /// </summary>
        /// <returns>Whether the test was skipped or not.</returns>
        public static bool SkipTestOnCI()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true")
                return false;
            Assert.True(true, "Skipped on CI.");
            return true;
        }

        #endregion

        #region File Verification Methods

        /// <summary>
        /// Verifies that the file content is the same as the expected file content after download.
        /// </summary>
        /// <param name="actualPath">The path of the file that has been downloade completely.</param>
        /// <param name="expectedPath">The path of the test file.</param>
        public static void VerifyFileContent(string actualPath, string expectedPath)
        {
            File.Exists(actualPath).Should().BeTrue();
            // If the file's hash is equal to the expected hash, then the file content must be the same.
            VerifyFileSHA512(actualPath, GetFileSHA512(expectedPath));
            File.Delete(actualPath);
        }

        /// <summary>
        /// Verifies that the file is empty after download.
        /// </summary>
        /// <param name="path"></param>
        public static void VerifyEmptyFile(string path)
        {
            File.Exists(path).Should().BeTrue();
            new FileInfo(path).Length.Should().Be(0);
            File.Delete(path);
        }

        public static void VerifyFileSHA512(string filePath, string expectedSHA512)
        {
            string actualSHA512 = GetFileSHA512(filePath);
            // Assert that the actual SHA512 hash matches the expected hash
            actualSHA512.Should().NotBeNullOrEmpty();
            actualSHA512.Should().Be(expectedSHA512);
        }

        private static string GetFileSHA512(string filePath)
        {
            using (SHA512 hasher = SHA512.Create())
            using (FileStream file = File.OpenRead(filePath))
            {
                // Compute the hash of the file
                byte[] hashBytes = hasher.ComputeHash(file);

                // Using StringBuilder to build the hexadecimal string
                StringBuilder sb = new StringBuilder(hashBytes.Length * 2);
                foreach (byte b in hashBytes)
                {
                    // Convert each byte to a two-digit hexadecimal string
                    sb.Append(b.ToString("x2"));
                }

                // Convert the StringBuilder to a string and return it.
                return sb.ToString();
            }
        }
        #endregion
    }
}
