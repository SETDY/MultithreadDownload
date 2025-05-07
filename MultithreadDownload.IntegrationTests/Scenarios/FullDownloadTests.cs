using FluentAssertions;
using MultithreadDownload.Core;
using MultithreadDownload.IntegrationTests.Fixtures;
using MultithreadDownload.Protocols;
using MultithreadDownload.Tasks;
using MultithreadDownload.Utils;
using System.Threading.Tasks;

namespace MultithreadDownload.IntegrationTests.Scenarios
{
    /// <summary>
    /// This class contains tests for the full download functionality.
    /// </summary>
    public class FullDownloadTests
    {

        [Fact]
        public async Task DownloadFile_SingleThread_FromLocalHttpServer_WorksCorrectly()
        {
            // Arrage
            // Create a test server that returns a known file size
            // Get context for the download task
            string downloadPath = Path.Combine(Path.GetTempPath(), "output.txt");
            (LocalHttpFileServer server, MultiDownload downloadManager, HttpDownloadContext? context) 
                = await TestHelper.PrepareDownload(
                DownloadServiceType.Http,
                1,
                1,
                downloadPath,
                TestConstants.SMALL_TESTFILE_PATH
            );
            downloadManager.TasksProgressCompleted += (sender, e) =>
            {
                // Assert => Check if the file exists and its content is correct
                TestHelper.VerifyFileContent(downloadPath, TestConstants.SMALL_TESTFILE_PATH);
                server.Stop();
            };
            server.Start();

            // Act
            downloadManager.AddTask(context);
            downloadManager.StartAllocator();
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(32)]
        public async Task DownloadFile_MultithreadThread_FromLocalHttpServer_WorksCorrectly(byte maxThreads)
        {
            // Since Github has limitations on the size of the file that can be saved,
            // the test file is not uploaded to the repository.
            // Therefore, this test will be skipped when running in Github Actions.
            // Since Skip.If() method has a issue with the Xunit test runner,
            // we use SkipTestOnCI() and Assert.True() to skip the test.
            if (TestHelper.SkipTestOnCI()) { return; }

            // Arrage:
            // Create a test server that returns a known file size
            // Get context for the download task
            string downloadPath = PathHelper.GetUniqueFileName(Path.GetTempPath(), "largeFile.test");
            (LocalHttpFileServer server, MultiDownload downloadManager, HttpDownloadContext? context)
                = await TestHelper.PrepareDownload(
                    DownloadServiceType.Http,
                    1,
                    maxThreads,
                    downloadPath,
                    TestConstants.LARGE_TESTFILE_PATH
            );
            downloadManager.TasksProgressCompleted += (sender, e) =>
            {
                // Assert
                TestHelper.VerifyFileContent(downloadPath, TestConstants.LARGE_TESTFILE_PATH);
                server.Stop();
            };
            server.Start();

            // Act
            downloadManager.AddTask(context);
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

        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        public async Task DownloadFile_EmptyFile_WorksCorrectly(byte maxDownloadThreads)
        {
            // Arrange
            // Create a test server that returns an empty file
            // Get context for the download task
            string emptyFilePath = Path.Combine("Resources", "emptyfile.txt");
            File.WriteAllText(emptyFilePath, string.Empty);

            string outputPath = Path.Combine(Path.GetTempPath(), "empty_output.txt");

            (LocalHttpFileServer server, MultiDownload downloadManager, HttpDownloadContext? context)
                = await TestHelper.PrepareDownload(
                    DownloadServiceType.Http,
                    1,
                    maxDownloadThreads,
                    outputPath,
                    emptyFilePath
            );
            downloadManager.TasksProgressCompleted += (sender, e) =>
            {
                // Assert
                TestHelper.VerifyEmptyFile(outputPath);
                server.Stop();
            };
            server.Start();

            // Assert
            context.Should().NotBeNull();

            // Act
            downloadManager.AddTask(context);
            downloadManager.StartAllocator();
        }
    }
}