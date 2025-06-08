using FluentAssertions;
using MultithreadDownload.Core;
using MultithreadDownload.IntegrationTests.Fixtures;
using MultithreadDownload.Protocols.Http;
using MultithreadDownload.Tasks;
using MultithreadDownload.Primitives;

namespace MultithreadDownload.IntegrationTests.Scenarios
{
    /// <summary>
    /// Integration tests for the whole download process with only multi task in real download scenarios.
    /// These tests validate the MultiDownload's behavior during actual file downloads
    /// with various configurations and network conditions.
    /// </summary>
    public class ConcurrentTaskDownloadTests
    {

        [Theory]
        [InlineData(1, 2)]
        [InlineData(1, 3)]
        [InlineData(2, 4)]
        [InlineData(3, 8)]
        [InlineData(4, 16)]
        public async Task DownloadFile_MultiTask_MultiThread_FromLocalHttpServer_WorksCorrectly
            (byte concurrentTasks, byte maxDownloadThread)
        {
            // Since Github has limitations on the size of the file that can be saved,
            // the test file is not uploaded to the repository.
            // Therefore, this test will be skipped when running in Github Actions.
            // Since Skip.If() method has a issue with the Xunit test runner,
            // we use TestHelper.SkipTestOnCI() and Assert.True() to skip the test.
            if (TestHelper.SkipTestOnCI()) { return; }

            // Arrange
            byte completedTasks = 0;
            (var server, var downloadManager, var url) = TestHelper.PrepareDownload(
                DownloadServiceType.Http, 1, TestConstants.LARGE_TESTFILE_PATH);
            // Get download task context (including segment information, etc.)
            List<HttpDownloadContext> contexts = new List<HttpDownloadContext>();
            for (int i = 0; i < concurrentTasks; i++)
            {
                contexts.Add( await TestHelper.GetHttpDownloadContext
                    (maxDownloadThread, url, Path.Combine(Path.GetTempPath(), $"output_{i}.txt")));
            }
            downloadManager.TasksProgressCompleted += (sender, e) =>
            {
                completedTasks++;
                // Check if all tasks are completed
                if (completedTasks != concurrentTasks) { return; }
                contexts.ForEach(c =>
                {
                    // Assert => Check if the file exists and its content is correct
                    TestHelper.VerifyFileContent(c.TargetPath, TestConstants.LARGE_TESTFILE_PATH);
                });
            };
            server.Start();

            // Act => Add tasks to the download manager and start the download allocator
            for (int i = 0; i < concurrentTasks; i++)
            {
                downloadManager.AddTask(contexts[i]);
            }
            // Start the download allocator
            downloadManager.StartAllocator();
        }
    }
}