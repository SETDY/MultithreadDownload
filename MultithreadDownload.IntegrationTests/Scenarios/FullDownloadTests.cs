using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using MultithreadDownload.Core;
using MultithreadDownload.IntegrationTests.Fixtures;
using MultithreadDownload.Logging;
using MultithreadDownload.Protocols;
using MultithreadDownload.Tasks;
using MultithreadDownload.Utils;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace MultithreadDownload.IntegrationTests.Scenarios
{
    /// <summary>
    /// This class contains tests for the full download functionality.
    /// </summary>
    public class FullDownloadTests
    {
        private readonly ITestOutputHelper _output;
        public FullDownloadTests(ITestOutputHelper output)
        {
            _output = output;
            DownloadLogger.Current = new TestOutputLogger(_output); // 启用测试日志输出
        }

        [Fact]
        public async Task DownloadFile_SingleThread_FromInternet_WrokCorrectly()
        {
            // Arrange
            // Get context for the download taskwith a single thread
            string url = "https://builds.dotnet.microsoft.com/dotnet/Sdk/9.0.300/dotnet-sdk-9.0.300-win-x64.exe";
            var downloadContext = await HttpDownloadContext.GetDownloadContext(1, Path.GetTempPath(), url);

            // Assert
            // The context should not be null and should be successful
            downloadContext.Value.Should().NotBeNull();
            downloadContext.IsSuccess.Should().BeTrue();
            downloadContext.ErrorMessage.Should().BeNull();

            // Create a download manager with a single parallel task
            MultiDownload downloadManager = new MultiDownload(1, DownloadServiceType.Http);
            // Create a TaskCompletionSource to wait for the download completion
            // It must be used to prevent the test from finishing before the download is completed
            TaskCompletionSource completionSource = new TaskCompletionSource();
            // Set the event handler for the download manager to handle the progress completion
            downloadManager.TasksProgressCompleted += (sender, e) =>
            {
                // Assert
                // Check if the file exists and its content is correct
                File.Exists(downloadContext.Value.TargetPath).Should().BeTrue();
                TestHelper.VerifyFileSHA512(downloadContext.Value.TargetPath, "5d58e5b1b40ffbd87d99eabaa30ff55baafb0318e35f38e0e220ac3630974a652428284e3ceb8841bf1a2c90aff0f6e7dfd631ca36f1b65ee1efd638fc68b0c8");
            };


            // Act
            // Add the download task that is created by the download context to the download manager
            downloadManager.AddTask(downloadContext.Value);

            // Assert
            // The download manager should have one task
            downloadManager.GetDownloadTasks().Count().Should().Be(1);

            // Act
            // Start the allocator of the download manager
            // There is a delay to prevent the process here is too fast that AddTask() cannot be completed (TODO:Maybe change it as await?)
            Thread.Sleep(1500); 
            downloadManager.StartAllocator();
            // Wait for the download to complete
            await completionSource.Task;
        }

        [Fact]
        public async Task DownloadFile_SingleThread_FromLocalHttpServer_WorksCorrectly()
        {
            // Arrange
            // Create a test server that returns a known file size
            // Get context for the download task
            string downloadPath = Path.Combine(Path.GetTempPath(), "output.txt");
            if (File.Exists(downloadPath))
                File.Delete(downloadPath);
            (LocalHttpFileServer server, MultiDownload downloadManager, HttpDownloadContext? context) 
                = await TestHelper.PrepareDownload(
                DownloadServiceType.Http,
                1,
                1,
                downloadPath,
                TestConstants.SMALL_TESTFILE_PATH
            );
            // Create a TaskCompletionSource to wait for the download completion
            // It must be used to prevent the test from finishing before the download is completed
            TaskCompletionSource completionSource = new TaskCompletionSource();

            downloadManager.TasksProgressCompleted += (sender, e) =>
            {
                try
                {
                    // Assert => Check if the file exists and its content is correct
                    TestHelper.VerifyFileContent(downloadPath, TestConstants.SMALL_TESTFILE_PATH);
                    completionSource.SetResult();
                }
                catch (Exception ex)
                {
                    completionSource.SetException(ex);
                }
                finally
                {
                    server.Stop();
                    File.Delete(downloadPath);
                }
            };
            server.Start();

            // Act
            downloadManager.AddTask(context);
            Thread.Sleep(1500);
            downloadManager.StartAllocator();
            // Wait for the download to complete
            await completionSource.Task;
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

            // Arrange:
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