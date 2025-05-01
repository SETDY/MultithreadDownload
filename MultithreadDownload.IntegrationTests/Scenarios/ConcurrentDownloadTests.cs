using FluentAssertions;
using MultithreadDownload.Core;
using MultithreadDownload.IntegrationTests.Fixtures;
using MultithreadDownload.Protocols;
using MultithreadDownload.Tasks;
using MultithreadDownload.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.IntegrationTests.Scenarios
{
    /// <summary>
    /// This class contains tests for the concurrent download functionality.
    /// </summary>
    public class ConcurrentDownloadTests
    {
        private const string LARGE_TESTFILE_PATH = "Resources\\testfile.test";

        [SkippableTheory]
        [InlineData(1, 2, "http://localhost:7000/")]
        [InlineData(1, 3, "http://localhost:7001/")]
        [InlineData(2, 4, "http://localhost:7002/")]
        [InlineData(3, 8, "http://localhost:7003/")]
        [InlineData(4, 16, "http://localhost:7004/")]
        [InlineData(8, 32, "http://localhost:7005/")]
        public async Task DownloadFile_MultiTask_MultiThread_FromLocalHttpServer_WorksCorrectly
            (byte maxParallelTasks, byte concurrentTasks, string prefixUrl)
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
            var server = new LocalHttpFileServer(prefixUrl, LARGE_TESTFILE_PATH);
            server.Start();
            byte MAX_PARALLEL_THREADS = 8;
            byte completedTasks = 0;

            // Act
            // Create a download service manager (currently using HTTP protocol)
            // Set up event handlers for progress and completion
            MultiDownload downloadManager = new MultiDownload(maxParallelTasks, DownloadServiceType.Http);
            // Get download task context (including segment information, etc.)
            List<HttpDownloadContext> contexts = new List<HttpDownloadContext>();
            for (int i = 0; i < concurrentTasks; i++)
            {
                Result<HttpDownloadContext> context = await HttpDownloadContext.GetDownloadContext(
                    MAX_PARALLEL_THREADS, Path.Combine(Path.GetTempPath(), $"output_{i}.txt"), prefixUrl);

                // Assert
                context.Should().NotBeNull();
                context.IsSuccess.Should().BeTrue();
                context.Value.Should().NotBeNull();

                contexts.Add(context.Value);
            }


            downloadManager.TasksProgressCompleted += (sender, e) =>
            {
                completedTasks++;
                if (completedTasks == concurrentTasks)
                {
                    foreach (HttpDownloadContext context in contexts)
                    {
                        File.Exists(context.TargetPath).Should().BeTrue();
                        File.Delete(context.TargetPath);
                    }
                }
            };

            // Act
            for (int i = 0; i < concurrentTasks; i++)
            {
                downloadManager.AddTask(contexts[i]);
            }

            // Start the download service manager
            downloadManager.StartAllocator();
        }
    }
}
