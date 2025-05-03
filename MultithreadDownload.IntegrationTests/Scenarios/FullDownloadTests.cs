using FluentAssertions;
using MultithreadDownload.Core;
using MultithreadDownload.IntegrationTests.Fixtures;
using MultithreadDownload.Protocols;
using MultithreadDownload.Tasks;
using MultithreadDownload.Utils;

namespace MultithreadDownload.IntegrationTests.Scenarios
{
    /// <summary>
    /// This class contains tests for the full download functionality.
    /// </summary>
    public class FullDownloadTests
    {
        private const string SMALL_TESTFILE_PATH = "Resources\\testfile.txt";

        private const string LARGE_TESTFILE_PATH = "Resources\\testfile.test";

        [Fact]
        public async Task DownloadFile_SingleThread_FromLocalHttpServer_WorksCorrectly()
        {
            // Arrage
            var server = new LocalHttpFileServer("http://localhost:5999/", SMALL_TESTFILE_PATH);
            server.Start();
            byte MAX_PARALLEL_THREADS = 8;
            byte MAX_PARALLEL_TASKS = 1;

            // Act
            // Create a download service manager (currently using HTTP protocol)
            // Set up event handlers for progress and completion
            MultiDownload downloadManager = new MultiDownload(MAX_PARALLEL_TASKS, DownloadServiceType.Http);
            downloadManager.TasksProgressCompleted += (sender, e) =>
            {
                File.Exists("output.txt").Should().BeTrue();
                File.ReadAllText("output.txt").Should().Be(File.ReadAllText(SMALL_TESTFILE_PATH));
                File.Delete("output.txt");
                server.Stop();
            };
            // Get download task context (including segment information, etc.)
            Result<HttpDownloadContext> context = await HttpDownloadContext.GetDownloadContext(
                MAX_PARALLEL_THREADS, Path.Combine(Path.GetTempPath(), "output.txt"), "http://localhost:5999/");

            // Assert
            context.Should().NotBeNull();
            context.IsSuccess.Should().BeTrue();

            // Act
            downloadManager.AddTask(context.Value);
            // Start the download service manager
            downloadManager.StartAllocator();
        }

        [Theory]
        [InlineData(6000, 2)]
        [InlineData(6001, 3)]
        [InlineData(6002, 4)]
        [InlineData(6003, 8)]
        public async Task DownloadFile_MultithreadThread_FromLocalHttpServer_WorksCorrectly(int port, byte maxThreads)
        {
            // Since Github has limitations on the size of the file that can be saved,
            // the test file is not uploaded to the repository.
            // Therefore, this test will be skipped when running in Github Actions.
            // Since Skip.If() method has a issue with the Xunit test runner,
            // we use Assert.True() to skip the test.
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
            {
                Assert.True(true, "Skipped on CI.");
                return;
            }

            // Arrage
            string prefixUrl = "http://localhost:" + port + "/";
            var server = new LocalHttpFileServer(prefixUrl, LARGE_TESTFILE_PATH);
            server.Start();
            byte MAX_PARALLEL_TASKS = 1;
            string safePath = PathHelper.GetUniqueFileName(Path.GetTempPath(), "largeFile.test");

            // Create a download service manager (currently using HTTP protocol)
            // Set up event handlers for progress and completion
            MultiDownload downloadManager = new MultiDownload(MAX_PARALLEL_TASKS, DownloadServiceType.Http);
            downloadManager.TasksProgressCompleted += (sender, e) =>
            {
                File.Exists("output.txt").Should().BeTrue();
                File.ReadAllText("output.txt").Should().Be(File.ReadAllText(SMALL_TESTFILE_PATH));
                File.Delete("output.txt");
                server.Stop();
            };
            // Get download task context (including segment information, etc.)
            Result<HttpDownloadContext> context = await HttpDownloadContext.GetDownloadContext(
                maxThreads, safePath, prefixUrl);

            // Assert
            context.Should().NotBeNull();
            context.IsSuccess.Should().BeTrue();

            // Act
            downloadManager.AddTask(context.Value);
            // Start the download service manager
            downloadManager.StartAllocator();
        }

        [Fact]
        public async Task DownloadFile_InvalidUrl_ShouldFailGracefully()
        {
            // Arrange
            byte MAX_PARALLEL_THREADS = 4;
            byte MAX_PARALLEL_TASKS = 1;
            string invalidUrl = "http://wrongUrl/nonexistentfile.txt";
            string outputPath = Path.Combine(Path.GetTempPath(), "invalid_output.txt");

            // Act
            MultiDownload downloadManager = new MultiDownload(MAX_PARALLEL_TASKS, DownloadServiceType.Http);
            var contextResult = await HttpDownloadContext.GetDownloadContext(MAX_PARALLEL_THREADS, outputPath, invalidUrl);

            // Assert
            contextResult.Should().NotBeNull();
            contextResult.IsSuccess.Should().BeFalse();
            contextResult.Value.Should().BeNull();
        }

        [Fact]
        public async Task DownloadFile_EmptyFile_WorksCorrectly()
        {
            // Arrange
            string emptyFilePath = "Resources\\emptyfile.txt";
            File.WriteAllText(emptyFilePath, string.Empty);

            var server = new LocalHttpFileServer("http://localhost:6006/", emptyFilePath);
            server.Start();

            byte MAX_PARALLEL_THREADS = 4;
            byte MAX_PARALLEL_TASKS = 1;
            string outputPath = Path.Combine(Path.GetTempPath(), "empty_output.txt");

            // Act
            MultiDownload downloadManager = new MultiDownload(MAX_PARALLEL_TASKS, DownloadServiceType.Http);
            var contextResult = await HttpDownloadContext.GetDownloadContext(MAX_PARALLEL_THREADS, outputPath, "http://localhost:6006/");
            downloadManager.TasksProgressCompleted += (sender, e) =>
            {
                // Assert
                File.Exists(outputPath).Should().BeTrue();
                new FileInfo(outputPath).Length.Should().Be(0);
                File.Delete(outputPath);
                server.Stop();
            };

            // Assert
            contextResult.IsSuccess.Should().BeTrue();

            // Act
            downloadManager.AddTask(contextResult.Value);
            downloadManager.StartAllocator();
        }
    }
}