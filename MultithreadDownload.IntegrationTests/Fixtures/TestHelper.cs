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

namespace MultithreadDownload.IntegrationTests.Fixtures
{
    public static class TestHelper
    {
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
        PrepareDownload(DownloadServiceType downloadServiceType ,byte maxParallelTasks, byte maxDownloadThreads, string outputPath, string realFilePath)
        {
            (var server, var manager, var url) = PrepareDownload(downloadServiceType, maxParallelTasks, realFilePath);

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
        /// <returns></returns>
        public static  (LocalHttpFileServer Server, MultiDownload Manager, string url)
        PrepareDownload(DownloadServiceType downloadServiceType, byte maxParallelTasks, string realFilePath)
        {
            string url = TestHelper.GenerateTemporaryUrl();
            LocalHttpFileServer server = new LocalHttpFileServer(url, realFilePath);
            server.Start();

            MultiDownload downloadManager = new MultiDownload(maxParallelTasks, downloadServiceType);

            return (server, downloadManager, url);
        }

        public static async Task<HttpDownloadContext> GetHttpDownloadContext(byte maxDownloadThreads ,string url, string outputPath)
        {
            Result<HttpDownloadContext> contextResult =
                await HttpDownloadContext.GetDownloadContext(maxDownloadThreads, outputPath, url);
            contextResult.Should().NotBeNull();
            contextResult.IsSuccess.Should().BeTrue();
            contextResult.Value.Should().NotBeNull();
            return contextResult.Value;
        }

        /// <summary>
        /// Generates a temporary URL for the given file path.
        /// </summary>
        /// <param name="realFilePath">The real file path to be used in the URL.</param>
        /// <returns>The temporary URL.</returns>
        public static string GenerateTemporaryUrl()
        {
            return $"http://localhost:{GetFreePort()}/";
        }

        /// <summary>
        /// Gets a free port on the local machine.
        /// </summary>
        /// <returns>The free port number.</returns>
        private static int GetFreePort()
        {
            // 0 represents the system to automatically assign a free port
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public static bool SkipTestOnCI()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true")
            {
                return false;
            }
            Assert.True(true, "Skipped on CI.");
            return true;

        }

        /// <summary>
        /// Verifies that the file content is the same as the expected file content after download.
        /// </summary>
        /// <param name="actualPath">The path of the file that has been downloade completely.</param>
        /// <param name="expectedPath">The path of the test file.</param>
        public static void VerifyFileContent(string actualPath, string expectedPath)
        {
            File.Exists(actualPath).Should().BeTrue();
            File.ReadAllText(actualPath).Should().Be(File.ReadAllText(expectedPath));
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
            string actualSHA512 = "";
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

                // Convert the StringBuilder to a string
                actualSHA512 = sb.ToString();
            }

            // Assert that the actual SHA512 hash matches the expected hash
            actualSHA512.Should().NotBeNullOrEmpty();
            actualSHA512.Should().Be(expectedSHA512);
        }

    }
}
