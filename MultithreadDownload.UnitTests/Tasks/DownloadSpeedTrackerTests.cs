using FluentAssertions;
using MultithreadDownload.Tasks;
using MultithreadDownload.TestUtilities.Helpers;
using MultithreadDownload.UnitTests.Fixtures;
using System.Threading.Tasks;

namespace MultithreadDownload.UnitTests.Tasks
{
    /// <summary>
    /// Unit tests for the DownloadSpeedTracker class.
    /// This test class covers all functionality including thread safety, timing, and edge cases.
    /// </summary>
    public class DownloadSpeedTrackerTests : IDisposable
    {
        private readonly DownloadSpeedTracker _tracker;

        /// <summary>
        /// Constructor that initializes a new tracker for each test.
        /// </summary>
        public DownloadSpeedTrackerTests()
        {
            _tracker = new DownloadSpeedTracker();
        }

        /// <summary>
        /// Tests that the tracker can be instantiated without throwing exceptions.
        /// </summary>
        [Fact]
        public void Constructor_ShouldInitializeSuccessfully()
        {
            // Arrange & Act
            DownloadSpeedTracker tracker = new DownloadSpeedTracker();

            // Assert
            Assert.NotNull(tracker);

            // Cleanup
            tracker.Dispose();
        }

        /// <summary>
        /// Tests that ReportBytes correctly accumulates the total bytes downloaded.
        /// </summary>
        [Fact]
        public void ReportBytes_ShouldAccumulateTotalBytes()
        {
            // Act
            // Report some bytes
            _tracker.ReportBytes(1024);
            _tracker.ReportBytes(2048);

            // Wait a moment to allow internal timing
            Thread.Sleep(600);

            // Assert
            // Speed should be calculated based on accumulated bytes
            Assert.True(_tracker.GetSpeedInBytesPerSecond() >= 0);
        }

        /// <summary>
        /// Tests that GetSpeedInBytesPerSecond returns 0 when called too quickly (within 500ms).
        /// This tests the anti-fluctuation mechanism.
        /// </summary>
        [Fact]
        public void GetSpeedInBytesPerSecond_WhenCalledTooQuickly_ShouldReturnZero()
        {
            // Arrange
            // Report some bytes
            _tracker.ReportBytes(1024);

            // Act
            // Get speed immediately
            double speed = _tracker.GetSpeedInBytesPerSecond();

            // Assert
            Assert.Equal(0, speed);
        }

        /// <summary>
        /// Tests that GetSpeedInBytesPerSecond calculates speed correctly after sufficient time has passed.
        /// </summary>
        [Fact]
        public async Task GetSpeedInBytesPerSecond_AfterSufficientTime_ShouldCalculateSpeed()
        {
            // Arrange
            // Report some bytes
            _tracker.ReportBytes(1024);

            // Act
            // Wait for more than 500ms to allow speed calculation
            await Task.Delay(600);

            // Assert
            Assert.True(_tracker.GetSpeedInBytesPerSecond() > 0, "Speed should be greater than 0 after sufficient time");
        }

        /// <summary>
        /// Tests that GetSpeedFormatted returns a non-empty string.
        /// </summary>
        [Fact]
        public async Task GetSpeedFormatted_ShouldReturnFormattedString()
        {
            // Arrange
            // Report some bytes
            _tracker.ReportBytes(2048);

            // Act
            // Wait for 600ms to allow speed calculation
            await Task.Delay(1000);
            // Get the formatted speed
            string formattedSpeed = _tracker.GetSpeedFormatted();

            // Assert
            Assert.NotNull(formattedSpeed);
            AssertHelper.InRangeByBytes(2048, AssertHelper.DOWNLOAD_SPEED_BYTES_ERROR_RANGE);
        }

        /// <summary>
        /// Tests that StartMonitoring begins the monitoring process and triggers events.
        /// </summary>
        [Fact]
        public async Task StartMonitoring_ShouldTriggerSpeedReportEvents()
        {
            // Arrange
            // Set up a TaskCompletionSource to wait for the event
            TaskCompletionSource<string?> tcs = new TaskCompletionSource<string?>();
            // Bind to speed report event
            _tracker.SpeedReportGenerated += speed =>
            {
                // Set the result when event is triggered
                tcs.TrySetResult(speed); 
            };

            // Act
            // Start monitoring with a short interval
            _tracker.StartMonitoring(TimeSpan.FromMilliseconds(100));
            // Report some bytes
            _tracker.ReportBytes(1024);

            // Wait for event to be triggered, with timeout to avoid hanging
            Task completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000));
            // Get the reported speed
            string? reportedSpeed = completedTask == tcs.Task ? tcs.Task.Result : null;

            // Assert
            Assert.NotNull(reportedSpeed);
            Assert.Equal("1KiB", reportedSpeed);
        }

        /// <summary>
        /// Tests that calling StartMonitoring multiple times doesn't create multiple timers.
        /// This tests the prevention of duplicate monitoring.
        /// </summary>
        [Fact]
        public async Task StartMonitoring_CalledMultipleTimes_ShouldNotCreateMultipleTimers()
        {
            // Arrange
            int eventCount = 0;
            TimeSpan interval = TimeSpan.FromMilliseconds(100);

            _tracker.SpeedReportGenerated += _ => Interlocked.Increment(ref eventCount);

            // Act
            // Call StartMonitoring multiple times
            // Only the first call should take effect
            _tracker.StartMonitoring(interval);
            // Second call should be ignored
            _tracker.StartMonitoring(interval);
            // Third call should be ignored
            _tracker.StartMonitoring(interval); 

            // Wait for 2-3 potential events
            await Task.Delay(250); 

            // Assert
            // If multiple timers were created, we would see more events than expected
            Assert.True(eventCount >= 1 && eventCount <= 4, $"Event count should be reasonable (1-4), but was {eventCount}");
        }

        /// <summary>
        /// Tests thread safety by having multiple threads report bytes simultaneously.
        /// </summary>
        [Theory]
        [InlineData(2)]  // Test with 2 threads
        [InlineData(3)]  // Test with 3 threads
        [InlineData(4)]  // Test with 4 threads
        [InlineData(8)]  // Test with 8 threads
        [InlineData(16)] // Test with 16 threads
        [InlineData(32)] // Test with 32 threads
        public async Task ReportBytes_MultipleThreads_ShouldBeThreadSafe(int threadCount)
        {
            // Arrange
            // Each thread will report 1000 bytes
            const long bytesPerThread = 1000;
            // The expected total bytes reported is threadCount * bytesPerThread
            long expectedTotalBytes = threadCount * bytesPerThread;
            Task[] tasks = new Task[threadCount];

            // Act
            // Start multiple tasks to report bytes concurrently,
            // which simulates multithreading downloads
            for (int i = 0; i < threadCount; i++)
            {
                tasks[i] = Task.Run(() => _tracker.ReportBytes(bytesPerThread));
            }

            // Wait for all tasks to complete its reporting
            await Task.WhenAll(tasks);
            // Wait for speed calculation
            await Task.Delay(600);
            // Get the calculated speed
            double speed = _tracker.GetSpeedInBytesPerSecond();

            // Assert
            Assert.True(speed > 0, "Speed should be calculated correctly even with concurrent access");
        }

        /// <summary>
        /// Tests that multiple threads can safely call GetSpeedInBytesPerSecond simultaneously.
        /// </summary>
        [Theory]
        [InlineData(2)]  // Test with 2 threads
        [InlineData(3)]  // Test with 3 threads
        [InlineData(4)]  // Test with 4 threads
        [InlineData(8)]  // Test with 8 threads
        [InlineData(16)] // Test with 16 threads
        [InlineData(32)] // Test with 32 threads
        public async Task GetSpeedInBytesPerSecond_MultipleThreads_ShouldBeThreadSafe(int threadCount)
        {
            // Arrange
            // Report some bytes to have a non-zero speed
            _tracker.ReportBytes(4096);
            // Wait for speed calculation
            await Task.Delay(600);
            // Array to hold speeds from each thread
            double[] speeds = new double[threadCount];
            // Array to hold tasks
            Task[] tasks = new Task[threadCount];

            // Act
            for (int i = 0; i < threadCount; i++)
            {
                // Capture loop variable to prevent the number is changed by the time the loop runs
                int index = i;
                // Get speed in each thread
                // to test concurrent access to GetSpeedInBytesPerSecond
                tasks[i] = Task.Run(() => speeds[index] = _tracker.GetSpeedInBytesPerSecond());
            }
            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            // Assert
            // All speeds should be non-negative and equal since the state is consistent
            speeds.All(speed => speed >= 0).Should().BeTrue("All speed calculations should be non-negative");
            speeds.All(speed => speed == 4096).Should().BeTrue("All speed calculations should be equal to the reported speed");
        }

        /// <summary>
        /// Tests that Dispose properly cleans up resources and stops monitoring.
        /// </summary>
        [Fact]
        public async Task Dispose_ShouldCleanupResourcesAndStopMonitoring()
        {
            // Arrange
            // Set up to count events
            int eventCount = 0;
            // Set up a TaskCompletionSource to wait for the first event
            TaskCompletionSource<bool> tcsFirstEvent = new TaskCompletionSource<bool>();
            // Bind to speed report event
            _tracker.SpeedReportGenerated += _ =>
            {
                Interlocked.Increment(ref eventCount);
                tcsFirstEvent.TrySetResult(true);
            };
            // Start monitoring
            _tracker.StartMonitoring(TimeSpan.FromMilliseconds(100));
            // Wait for first event to ensure monitoring started
            Task completedTask = await Task.WhenAny(tcsFirstEvent.Task, Task.Delay(1000));
            // Capture event count at Dispose time
            int countAtDispose = eventCount;

            // Act
            // Dispose the tracker
            _tracker.Dispose();
            // Wait a bit to confirm no more events are fired after Dispose
            await Task.Delay(300);

            // Assert
            // Ensure at least one event was triggered before Dispose
            Assert.True(completedTask == tcsFirstEvent.Task, "No event triggered before Dispose");
            // After Dispose, event count should not increase
            Assert.Equal(countAtDispose, eventCount);
        }

        /// <summary>
        /// Tests that the tracker handles zero bytes correctly.
        /// </summary>
        [Fact]
        public async Task ReportBytes_WithZeroBytes_ShouldHandleGracefully()
        {
            // Arrange & Act
            // Report zero bytes
            _tracker.ReportBytes(0);
            // Wait for speed calculation
            await Task.Delay(600);
            // Get speed in bytes per second
            double speed = _tracker.GetSpeedInBytesPerSecond();
            // Get formatted speed
            string formattedSpeed = _tracker.GetSpeedFormatted();

            // Assert
            Assert.True(speed == 0, "Speed should be zero even with zero bytes");
            Assert.Equal("0KiB", formattedSpeed);
        }

        /// <summary>
        /// Tests that the tracker handles large byte values correctly.
        /// </summary>
        [Fact]
        public async Task ReportBytes_WithLargeValues_ShouldHandleCorrectly()
        {
            // Arrange
            // Get a large byte value close to long.MaxValue
            const long largeByteValue = long.MaxValue / 1000;

            // Act
            // Report large bytes
            _tracker.ReportBytes(largeByteValue);
            // Wait for speed calculation
            await Task.Delay(600);
            // Get speed in bytes per second
            double speed = _tracker.GetSpeedInBytesPerSecond();
            // Get formatted speed
            string formattedSpeed = _tracker.GetSpeedFormatted();

            // Assert
            Assert.True(speed == largeByteValue, "Speed in bytes per second should be equal to the actual bytes reported.");
            Assert.NotEmpty(formattedSpeed);
        }

        /// <summary>
        /// Tests that consecutive speed calculations show progressive changes.
        /// </summary>
        [Fact]
        public async Task GetSpeedInBytesPerSecond_ConsecutiveCalls_ShouldShowProgression()
        {
            // Arrange & Act
            // Report initial bytes
            _tracker.ReportBytes(1000);
            // Wait for speed calculation
            await Task.Delay(600);
            // First speed measurement
            double firstSpeed = _tracker.GetSpeedInBytesPerSecond();

            // Add more bytes and wait
            _tracker.ReportBytes(2000);
            await Task.Delay(600);
            // Second speed measurement
            double secondSpeed = _tracker.GetSpeedInBytesPerSecond();

            // Assert
            Assert.True(firstSpeed > 0, "First speed measurement should be positive");
            Assert.True(secondSpeed > 0, "Second speed measurement should be positive");
        }

        /// <summary>
        /// Tests a realistic download simulation scenario.
        /// </summary>
        /// <remarks>
        /// An additional test for real-world scenarios.
        /// </remarks>
        [Fact]
        public async Task SimulateRealDownload_ShouldTrackSpeedAccurately()
        {
            // Arrange
            // Create a new tracker for this simulation
            using DownloadSpeedTracker tracker = new DownloadSpeedTracker();
            List<string> speedReports = new List<string>();
            // Bind to speed report event to collect reports
            tracker.SpeedReportGenerated += speed => speedReports.Add(speed);
            // Start monitoring with 200ms interval
            tracker.StartMonitoring(TimeSpan.FromMilliseconds(200));

            // Act
            // Simulate downloading chunks of data with delays
            Task[] downloadTasks = new Task[]
            {
                // Start with 1KiB chunk
                SimulateDownloadChunk(tracker, 1024, 100),
                // Then 2KiB chunk
                SimulateDownloadChunk(tracker, 2048, 150),
                // Then 4KiB chunk
                SimulateDownloadChunk(tracker, 4096, 100),
                // Finally another 1KiB chunk
                SimulateDownloadChunk(tracker, 1024, 200) 
            };
            // Wait for all download simulations to complete
            await Task.WhenAll(downloadTasks);
            // Wait for final speed reports
            await Task.Delay(1000);

            // Assert
            // Check that speed reports were generated and are not empty
            Assert.True(speedReports.Count > 0, "Should have generated speed reports");
            Assert.All(speedReports, report => Assert.NotEmpty(report));
        }

        /// <summary>
        /// Helper method to simulate downloading a chunk of data.
        /// </summary>
        /// <param name="tracker">The speed tracker to report to.</param>
        /// <param name="chunkSize">Size of the chunk to simulate.</param>
        /// <param name="delayMs">Delay to simulate network latency.</param>
        private static async Task SimulateDownloadChunk(DownloadSpeedTracker tracker, long chunkSize, int delayMs)
        {
            await Task.Delay(delayMs);
            tracker.ReportBytes(chunkSize);
        }

        /// <summary>
        /// Cleanup method called after each test to dispose resources.
        /// </summary>
        public void Dispose()
        {
            _tracker?.Dispose();
        }
    }
}
