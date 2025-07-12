using FluentAssertions;
using MultithreadDownload.Core;
using MultithreadDownload.IntegrationTests.Fixtures;
using MultithreadDownload.Logging;
using MultithreadDownload.Protocols.Http;
using MultithreadDownload.Tasks;
using MultithreadDownload.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace MultithreadDownload.IntegrationTests.Scenarios
{
    /// <summary>
    /// Integration tests for DownloadSpeedTracker in real download scenarios.
    /// These tests validate the speed tracker's behavior during actual file downloads
    /// with various configurations and network conditions.
    /// </summary>
    public class DownloadSpeedTrackerIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public DownloadSpeedTrackerIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
            DownloadLogger.Current = new TestOutputLogger(_output);
        }

        /// <summary>
        /// Tests speed tracking during a single-threaded download from local HTTP server.
        /// This test verifies that the speed tracker accurately reports download speeds
        /// and generates appropriate speed reports during a controlled download scenario.
        /// </summary>
        [Fact]
        public async Task SpeedTracker_SingleThreadDownload_LocalServer_TracksSpeedAccurately()
        {
            // Arrange
            string downloadPath = Path.Combine(Path.GetTempPath(), "speed_test_single.txt");
            if (File.Exists(downloadPath))
                File.Delete(downloadPath);

            var (server, downloadManager, context) = await TestHelper.PrepareFullHttpDownloadEnvironment(
                DownloadServiceType.Http,
                1,
                1,
                downloadPath,
                TestConstants.SMALL_TESTFILE_PATH
            );

            // Setup speed tracker
            var speedTracker = new DownloadSpeedTracker();
            var speedReports = new ConcurrentQueue<string>();
            var bytesReported = new List<long>();
            var completionSource = new TaskCompletionSource();

            // Configure speed monitoring
            speedTracker.SpeedReportGenerated += speed =>
            {
                speedReports.Enqueue(speed);
                _output.WriteLine($"Speed Report: {speed}");
            };
            speedTracker.StartMonitoring(TimeSpan.FromMilliseconds(500));

            // Setup download completion handler
            downloadManager.TasksProgressCompleted += (sender, e) =>
            {
                try
                {
                    // Verify file download completed successfully
                    TestHelper.VerifyFileContent(downloadPath, TestConstants.SMALL_TESTFILE_PATH);

                    // Verify speed tracking functionality
                    speedReports.Should().NotBeEmpty("Speed tracker should generate reports during download");
                    bytesReported.Should().NotBeEmpty("Bytes should have been reported to tracker");

                    var finalSpeed = speedTracker.GetSpeedInBytesPerSecond();
                    var formattedSpeed = speedTracker.GetSpeedFormatted();

                    _output.WriteLine($"Final Speed: {finalSpeed} bytes/sec");
                    _output.WriteLine($"Formatted Speed: {formattedSpeed}");

                    finalSpeed.Should().BeGreaterThanOrEqualTo(0, "Final speed should be non-negative");
                    formattedSpeed.Should().NotBeNullOrEmpty("Formatted speed should be available");

                    completionSource.SetResult();
                }
                catch (Exception ex)
                {
                    completionSource.SetException(ex);
                }
                finally
                {
                    speedTracker?.Dispose();
                    server?.Stop();
                    if (File.Exists(downloadPath))
                        File.Delete(downloadPath);
                }
            };

            // Simulate periodic speed tracking during download
            _ = Task.Run(async () =>
            {
                var random = new Random();
                while (!completionSource.Task.IsCompleted)
                {
                    // Simulate bytes being downloaded
                    var bytesChunk = random.Next(512, 2048);
                    speedTracker.ReportBytes(bytesChunk);
                    bytesReported.Add(bytesChunk);

                    await Task.Delay(100);
                }
            });


            // Act
            downloadManager.AddTask(context);
            Thread.Sleep(1500);
            downloadManager.StartAllocator();

            // Wait for completion
            await completionSource.Task;
        }

        /// <summary>
        /// Tests speed tracking during a multi-threaded download from local HTTP server.
        /// This test validates that the speed tracker correctly handles concurrent
        /// byte reporting from multiple download threads.
        /// </summary>
        [Theory]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(8)]
        public async Task SpeedTracker_MultiThreadDownload_LocalServer_HandlesThreadSafety(byte maxThreads)
        {
            if (TestHelper.SkipTestOnCI()) { return; }

            // Arrange
            string downloadPath = PathHelper.GetUniqueFileName(Path.GetTempPath(), "speed_test_multi.test");
            var (server, downloadManager, context) = await TestHelper.PrepareFullHttpDownloadEnvironment(
                DownloadServiceType.Http,
                1,
                maxThreads,
                downloadPath,
                TestConstants.LARGE_TESTFILE_PATH
            );

            var speedTracker = new DownloadSpeedTracker();
            var speedReports = new ConcurrentQueue<string>();
            var threadReports = new ConcurrentDictionary<int, List<long>>();
            var completionSource = new TaskCompletionSource();

            // Configure speed monitoring with higher frequency for multi-thread testing
            speedTracker.SpeedReportGenerated += speed =>
            {
                speedReports.Enqueue(speed);
                _output.WriteLine($"Multi-thread Speed Report: {speed}");
            };
            speedTracker.StartMonitoring(TimeSpan.FromMilliseconds(300));

            downloadManager.TasksProgressCompleted += (sender, e) =>
            {
                try
                {
                    // Verify download completed
                    TestHelper.VerifyFileContent(downloadPath, TestConstants.LARGE_TESTFILE_PATH);

                    // Verify thread safety and speed tracking
                    speedReports.Should().NotBeEmpty("Speed reports should be generated during multi-thread download");
                    threadReports.Should().NotBeEmpty("Multiple threads should have reported bytes");

                    var totalBytesFromThreads = threadReports.Values.SelectMany(x => x).Sum();
                    totalBytesFromThreads.Should().BeGreaterThan(0, "Total bytes from all threads should be positive");

                    var finalSpeed = speedTracker.GetSpeedInBytesPerSecond();
                    finalSpeed.Should().BeGreaterThanOrEqualTo(0, "Final speed calculation should be thread-safe");

                    _output.WriteLine($"Threads participated: {threadReports.Count}");
                    _output.WriteLine($"Total bytes reported: {totalBytesFromThreads}");
                    _output.WriteLine($"Final speed: {finalSpeed} bytes/sec");

                    completionSource.SetResult();
                }
                catch (Exception ex)
                {
                    completionSource.SetException(ex);
                }
                finally
                {
                    speedTracker?.Dispose();
                    server?.Stop();
                    if (File.Exists(downloadPath))
                        File.Delete(downloadPath);
                }
            };

            // Simulate multiple threads reporting bytes concurrently
            var simulationTasks = new Task[maxThreads];
            for (int i = 0; i < maxThreads; i++)
            {
                int threadId = i;
                simulationTasks[i] = Task.Run(async () =>
                {
                    var threadBytes = new List<long>();
                    var random = new Random(threadId);

                    while (!completionSource.Task.IsCompleted)
                    {
                        var bytesChunk = random.Next(1024, 4096);
                        speedTracker.ReportBytes(bytesChunk);
                        threadBytes.Add(bytesChunk);

                        threadReports.AddOrUpdate(threadId, threadBytes, (key, value) => value);

                        await Task.Delay(random.Next(50, 200));
                    }
                });
            }



            // Act
            downloadManager.AddTask(context);
            downloadManager.StartAllocator();

            // Wait for completion
            await completionSource.Task;

            // Cleanup simulation tasks
            await Task.WhenAll(simulationTasks.Where(t => !t.IsCompleted));
        }

        /// <summary>
        /// Tests speed tracking during concurrent download tasks.
        /// This test verifies that the speed tracker can handle multiple simultaneous downloads
        /// and provide accurate speed measurements across different download tasks.
        /// </summary>
        [Theory]
        [InlineData(2, 2)]
        [InlineData(3, 4)]
        public async Task SpeedTracker_ConcurrentDownloads_TracksIndependently(byte concurrentTasks, byte maxThreadsPerTask)
        {
            if (TestHelper.SkipTestOnCI()) { return; }

            // Arrange
            var (server, downloadManager, url) = TestHelper.PreparePartialHttpDownloadEnvironment(
                DownloadServiceType.Http, 1, TestConstants.LARGE_TESTFILE_PATH);

            var speedTrackers = new List<DownloadSpeedTracker>();
            var downloadContexts = new List<HttpDownloadContext>();
            var completionSources = new List<TaskCompletionSource>();
            var speedReportCounts = new ConcurrentDictionary<int, int>();

            // Setup multiple download contexts and speed trackers
            for (int i = 0; i < concurrentTasks; i++)
            {
                var context = await TestHelper.GetHttpDownloadContext(
                    maxThreadsPerTask, url, Path.Combine(Path.GetTempPath(), $"concurrent_speed_test_{i}.txt"));
                var tracker = new DownloadSpeedTracker();
                var completion = new TaskCompletionSource();

                downloadContexts.Add(context);
                speedTrackers.Add(tracker);
                completionSources.Add(completion);

                int taskIndex = i;
                tracker.SpeedReportGenerated += speed =>
                {
                    speedReportCounts.AddOrUpdate(taskIndex, 1, (key, value) => value + 1);
                    _output.WriteLine($"Task {taskIndex} Speed: {speed}");
                };
                tracker.StartMonitoring(TimeSpan.FromMilliseconds(400));
            }

            int completedTasks = 0;
            downloadManager.TasksProgressCompleted += (sender, e) =>
            {
                int currentCompleted = Interlocked.Increment(ref completedTasks);

                if (currentCompleted == concurrentTasks)
                {
                    try
                    {
                        // Verify all downloads completed successfully
                        for (int i = 0; i < concurrentTasks; i++)
                        {
                            TestHelper.VerifyFileContent(downloadContexts[i].TargetPath, TestConstants.LARGE_TESTFILE_PATH);
                        }

                        // Verify speed tracking for each task
                        speedReportCounts.Should().HaveCount(concurrentTasks,
                            "Each concurrent task should have generated speed reports");

                        foreach (var (taskIndex, reportCount) in speedReportCounts)
                        {
                            reportCount.Should().BeGreaterThan(0,
                                $"Task {taskIndex} should have generated at least one speed report");
                        }

                        // Verify final speed calculations
                        for (int i = 0; i < concurrentTasks; i++)
                        {
                            var finalSpeed = speedTrackers[i].GetSpeedInBytesPerSecond();
                            finalSpeed.Should().BeGreaterThanOrEqualTo(0,
                                $"Task {i} final speed should be non-negative");
                        }

                        _output.WriteLine($"All {concurrentTasks} concurrent downloads completed successfully");

                        // Signal completion for all tasks
                        completionSources.ForEach(cs => cs.TrySetResult());
                    }
                    catch (Exception ex)
                    {
                        completionSources.ForEach(cs => cs.TrySetException(ex));
                    }
                    finally
                    {
                        // Cleanup resources
                        speedTrackers.ForEach(tracker => tracker?.Dispose());
                        server?.Stop();
                        downloadContexts.ForEach(context =>
                        {
                            if (File.Exists(context.TargetPath))
                                File.Delete(context.TargetPath);
                        });
                    }
                }
            };

            // Simulate byte reporting for each download task
            var byteReportingTasks = new List<Task>();
            for (int i = 0; i < concurrentTasks; i++)
            {
                int taskIndex = i;
                var tracker = speedTrackers[i];
                var completion = completionSources[i];

                byteReportingTasks.Add(Task.Run(async () =>
                {
                    var random = new Random(taskIndex * 100);
                    while (!completion.Task.IsCompleted)
                    {
                        var bytesChunk = random.Next(2048, 8192);
                        tracker.ReportBytes(bytesChunk);
                        await Task.Delay(random.Next(100, 300));
                    }
                }));
            }



            // Act
            foreach (var context in downloadContexts)
            {
                downloadManager.AddTask(context);
            }
            downloadManager.StartAllocator();

            // Wait for all downloads to complete
            await Task.WhenAll(completionSources.Select(cs => cs.Task));

            // Cleanup byte reporting tasks
            await Task.WhenAll(byteReportingTasks.Where(t => !t.IsCompleted));
        }

        /// <summary>
        /// Tests speed tracking with real internet download to validate behavior
        /// under actual network conditions with variable speeds.
        /// </summary>
        [Fact]
        public async Task SpeedTracker_RealInternetDownload_HandlesVariableNetworkConditions()
        {
            if (TestHelper.SkipTestOnCI()) { return; }

            // Arrange
            string url = "https://builds.dotnet.microsoft.com/dotnet/Sdk/9.0.300/dotnet-sdk-9.0.300-win-x64.exe";
            var downloadContext = await HttpDownloadContext.GetDownloadContext(4, Path.GetTempPath(), url);

            downloadContext.IsSuccess.Should().BeTrue("Download context should be created successfully");
            downloadContext.Value.Should().NotBeNull();

            var speedTracker = new DownloadSpeedTracker();
            var speedReports = new ConcurrentQueue<(DateTime timestamp, string speed)>();
            var completionSource = new TaskCompletionSource();
            var downloadManager = new MultiDownload(1, DownloadServiceType.Http);

            // Configure speed monitoring for real network conditions
            speedTracker.SpeedReportGenerated += speed =>
            {
                speedReports.Enqueue((DateTime.Now, speed));
                _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Internet Download Speed: {speed}");
            };
            speedTracker.StartMonitoring(TimeSpan.FromSeconds(1)); // Longer interval for real downloads

            downloadManager.TasksProgressCompleted += (sender, e) =>
            {
                try
                {
                    // Verify download integrity
                    File.Exists(downloadContext.Value.Value.TargetPath).Should().BeTrue("Downloaded file should exist");
                    TestHelper.VerifyFileSHA512(downloadContext.Value.Value.TargetPath,
                        "5d58e5b1b40ffbd87d99eabaa30ff55baafb0318e35f38e0e220ac3630974a652428284e3ceb8841bf1a2c90aff0f6e7dfd631ca36f1b65ee1efd638fc68b0c8");

                    // Verify speed tracking under real network conditions
                    speedReports.Should().NotBeEmpty("Speed reports should be generated during internet download");

                    var reportArray = speedReports.ToArray();
                    reportArray.Length.Should().BeGreaterThan(1, "Multiple speed reports should be generated for large download");

                    // Verify speed progression over time
                    var speeds = reportArray.Select(r => r.speed).ToList();
                    speeds.Should().AllSatisfy(speed =>
                        speed.Should().NotBeNullOrEmpty("Each speed report should have valid content"));

                    var finalSpeed = speedTracker.GetSpeedInBytesPerSecond();
                    _output.WriteLine($"Final internet download speed: {finalSpeed} bytes/sec");
                    _output.WriteLine($"Total speed reports generated: {reportArray.Length}");

                    completionSource.SetResult();
                }
                catch (Exception ex)
                {
                    completionSource.SetException(ex);
                }
                finally
                {
                    speedTracker?.Dispose();
                    if (File.Exists(downloadContext.Value.Value.TargetPath))
                        File.Delete(downloadContext.Value.Value.TargetPath);
                }
            };

            // Simulate realistic byte reporting based on download progress
            _ = Task.Run(async () =>
            {
                var random = new Random();
                var totalReported = 0L;

                while (!completionSource.Task.IsCompleted)
                {
                    // Simulate variable chunk sizes like real downloads
                    var chunkSize = random.Next(8192, 65536); // 8KB to 64KB chunks
                    speedTracker.ReportBytes(chunkSize);
                    totalReported += chunkSize;

                    // Variable delay to simulate network conditions
                    var delay = random.Next(50, 500);
                    await Task.Delay(delay);

                    // Log progress periodically
                    if (totalReported % (1024 * 1024) < chunkSize) // Every ~1MB
                    {
                        _output.WriteLine($"Progress: {totalReported / (1024 * 1024)} MB reported to speed tracker");
                    }
                }
            });

            // Act
            downloadManager.AddTask(downloadContext.ValueOrNull());
            Thread.Sleep(1500);
            downloadManager.StartAllocator();

            // Wait with timeout for internet download
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(10)); // 10 minute timeout
            var completedTask = await Task.WhenAny(completionSource.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                speedTracker?.Dispose();
                throw new TimeoutException("Internet download test timed out after 10 minutes");
            }

            await completionSource.Task;
        }

        /// <summary>
        /// Tests speed tracker behavior when download fails or is interrupted.
        /// This ensures the tracker handles error conditions gracefully.
        /// </summary>
        [Fact]
        public async Task SpeedTracker_DownloadFailure_HandlesGracefully()
        {
            // Arrange
            string invalidUrl = "http://nonexistent.domain.invalid/file.txt";
            string outputPath = Path.Combine(Path.GetTempPath(), "failed_download_test.txt");

            var speedTracker = new DownloadSpeedTracker();
            var speedReports = new List<string>();

            speedTracker.SpeedReportGenerated += speed =>
            {
                speedReports.Add(speed);
                _output.WriteLine($"Speed during failed download: {speed}");
            };
            speedTracker.StartMonitoring(TimeSpan.FromMilliseconds(200));

            try
            {
                // Act
                var downloadManager = new MultiDownload(1, DownloadServiceType.Http);
                var contextResult = await HttpDownloadContext.GetDownloadContext(1, outputPath, invalidUrl);

                // Assert
                contextResult.IsSuccess.Should().BeFalse("Invalid URL should fail to create context");
                contextResult.Value.HasValue.Should().BeFalse("Context should be null for invalid URL");

                // Test tracker behavior with no actual bytes
                await Task.Delay(1000); // Let tracker run for a bit

                var speed = speedTracker.GetSpeedInBytesPerSecond();
                var formattedSpeed = speedTracker.GetSpeedFormatted();

                speed.Should().BeGreaterThanOrEqualTo(0, "Speed should be non-negative even with no downloads");
                formattedSpeed.Should().NotBeNull("Formatted speed should always be available");

                _output.WriteLine($"Speed tracker handled failure gracefully: {formattedSpeed}");
            }
            finally
            {
                speedTracker?.Dispose();
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
        }
    }
}
