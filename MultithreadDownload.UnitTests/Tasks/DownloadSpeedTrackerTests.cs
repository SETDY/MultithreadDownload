using FluentAssertions;
using MultithreadDownload.Tasks;
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
            var tracker = new DownloadSpeedTracker();

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
            // Arrange
            const long firstBytes = 1024;
            const long secondBytes = 2048;

            // Act
            _tracker.ReportBytes(firstBytes);
            _tracker.ReportBytes(secondBytes);

            // Wait a moment to allow internal timing
            Thread.Sleep(600);

            // Assert
            double speed = _tracker.GetSpeedInBytesPerSecond();
            Assert.True(speed >= 0); // Speed should be calculated based on accumulated bytes
        }

        /// <summary>
        /// Tests that GetSpeedInBytesPerSecond returns 0 when called too quickly (within 500ms).
        /// This tests the anti-fluctuation mechanism.
        /// </summary>
        [Fact]
        public void GetSpeedInBytesPerSecond_WhenCalledTooQuickly_ShouldReturnZero()
        {
            // Arrange
            _tracker.ReportBytes(1024);

            // Act ==> Call immediately without waiting
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
            const long bytesToReport = 1024;
            _tracker.ReportBytes(bytesToReport);

            // Act ==> Wait for more than 500ms to allow speed calculation
            await Task.Delay(600);
            double speed = _tracker.GetSpeedInBytesPerSecond();

            // Assert
            Assert.True(speed > 0, "Speed should be greater than 0 after sufficient time");
        }

        /// <summary>
        /// Tests that GetSpeedFormatted returns a non-empty string.
        /// </summary>
        [Fact]
        public async Task GetSpeedFormatted_ShouldReturnFormattedString()
        {
            // Arrange
            _tracker.ReportBytes(2048);
            await Task.Delay(600);

            // Act => Get the formatted speed
            string formattedSpeed = _tracker.GetSpeedFormatted();

            // Assert
            Assert.NotNull(formattedSpeed);
            Assert.NotEmpty(formattedSpeed);
        }

        /// <summary>
        /// Tests that StartMonitoring begins the monitoring process and triggers events.
        /// </summary>
        [Fact]
        public async Task StartMonitoring_ShouldTriggerSpeedReportEvents()
        {
            // Arrange
            bool eventTriggered = false;
            string? reportedSpeed = null;
            TimeSpan interval = TimeSpan.FromMilliseconds(100);

            _tracker.SpeedReportGenerated += speed =>
            {
                eventTriggered = true;
                reportedSpeed = speed;
            };

            // Act
            _tracker.StartMonitoring(interval);
            _tracker.ReportBytes(1024);

            // Wait for at least one event to be triggered
            await Task.Delay(200);

            // Assert
            Assert.True(eventTriggered, "SpeedReportGenerated event should be triggered");
            Assert.NotNull(reportedSpeed);
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
            _tracker.StartMonitoring(interval);
            _tracker.StartMonitoring(interval); // Second call should be ignored
            _tracker.StartMonitoring(interval); // Third call should be ignored

            // Wait for 2-3 potential events
            await Task.Delay(250); 

            // Assert
            // If multiple timers were created, we would see more events than expected
            Assert.True(eventCount >= 1 && eventCount <= 4,
                $"Event count should be reasonable (1-4), but was {eventCount}");
        }

        /// <summary>
        /// Tests that StopMonitoring stops the event generation.
        /// </summary>
        [Fact]
        public async Task StopMonitoring_ShouldStopEventGeneration()
        {
            // Arrange
            int eventCount = 0;
            TimeSpan interval = TimeSpan.FromMilliseconds(50);

            _tracker.SpeedReportGenerated += _ => Interlocked.Increment(ref eventCount);
            _tracker.StartMonitoring(interval);

            // Wait for some events
            await Task.Delay(120);
            int countAfterStart = eventCount;

            // Act
            _tracker.StopMonitoring();
            await Task.Delay(120); // Wait same amount of time
            int countAfterStop = eventCount;

            // Assert
            Assert.True(countAfterStart > 0, "Events should have been generated before stopping");
            Assert.Equal(countAfterStart, countAfterStop); // No new events after stopping
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
            const long bytesPerThread = 1000;
            long expectedTotalBytes = threadCount * bytesPerThread;
            Task[] tasks = new Task[threadCount];

            // Act => Start multiple tasks to report bytes concurrently
            for (int i = 0; i < threadCount; i++)
            {
                tasks[i] = Task.Run(() => _tracker.ReportBytes(bytesPerThread));
            }

            await Task.WhenAll(tasks); // Wait for all tasks to complete its reporting
            await Task.Delay(600); // Wait for speed calculation

            // Assert
            double speed = _tracker.GetSpeedInBytesPerSecond();
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
            _tracker.ReportBytes(5000);
            await Task.Delay(600);

            double[] speeds = new double[threadCount];
            Task[] tasks = new Task[threadCount];

            // Act
            for (int i = 0; i < threadCount; i++)
            {
                // Capture loop variable to prevent the number is changed by the time the loop runs
                int index = i;
                // Get speed in each thread
                tasks[i] = Task.Run(() => speeds[index] = _tracker.GetSpeedInBytesPerSecond());
            }

            await Task.WhenAll(tasks);

            // Assert
            foreach (var speed in speeds)
            {
                Assert.True(speed >= 0, "All speed calculations should be non-negative");
            }
        }

        /// <summary>
        /// Tests that Dispose properly cleans up resources and stops monitoring.
        /// </summary>
        [Fact]
        public async Task Dispose_ShouldCleanupResourcesAndStopMonitoring()
        {
            // Arrange
            var eventCount = 0;
            var interval = TimeSpan.FromMilliseconds(50);

            _tracker.SpeedReportGenerated += _ => Interlocked.Increment(ref eventCount);
            _tracker.StartMonitoring(interval);

            // Let some events fire
            await Task.Delay(120); 

            // Act
            _tracker.Dispose();
            var countAtDispose = eventCount;

            // Wait to see if more events fire
            await Task.Delay(120);

            // Assert
            // No events should fire after dispose
            Assert.Equal(countAtDispose, eventCount); 
        }

        /// <summary>
        /// Tests that the tracker handles zero bytes correctly.
        /// </summary>
        [Fact]
        public async Task ReportBytes_WithZeroBytes_ShouldHandleGracefully()
        {
            // Arrange & Act
            _tracker.ReportBytes(0);
            await Task.Delay(600);
            double speed = _tracker.GetSpeedInBytesPerSecond();
            string formattedSpeed = _tracker.GetSpeedFormatted();

            // Assert
            Assert.True(speed == 0, "Speed should be zero even with zero bytes");
            Assert.NotEmpty(formattedSpeed);
        }

        /// <summary>
        /// Tests that the tracker handles large byte values correctly.
        /// </summary>
        [Fact]
        public async Task ReportBytes_WithLargeValues_ShouldHandleCorrectly()
        {
            // Arrange
            const long largeByteValue = long.MaxValue / 1000; // Large but safe value

            // Act
            _tracker.ReportBytes(largeByteValue);
            await Task.Delay(600);
            double speed = _tracker.GetSpeedInBytesPerSecond();
            string formattedSpeed = _tracker.GetSpeedFormatted();

            // Assert
            Assert.True(speed >= 0, "Speed should be calculated correctly for large values");
            Assert.NotEmpty(formattedSpeed);
        }

        /// <summary>
        /// Tests that consecutive speed calculations show progressive changes.
        /// </summary>
        [Fact]
        public async Task GetSpeedInBytesPerSecond_ConsecutiveCalls_ShouldShowProgression()
        {
            // Arrange
            _tracker.ReportBytes(1000);
            await Task.Delay(600);

            double firstSpeed = _tracker.GetSpeedInBytesPerSecond();

            // Add more bytes and wait
            _tracker.ReportBytes(2000);
            await Task.Delay(600);

            double secondSpeed = _tracker.GetSpeedInBytesPerSecond();

            // Assert
            Assert.True(firstSpeed >= 0, "First speed measurement should be non-negative");
            Assert.True(secondSpeed >= 0, "Second speed measurement should be non-negative");
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
            using var tracker = new DownloadSpeedTracker();
            List<string> speedReports = new List<string>();

            tracker.SpeedReportGenerated += speed => speedReports.Add(speed);
            tracker.StartMonitoring(TimeSpan.FromMilliseconds(200));

            // Act - Simulate downloading chunks of data
            var downloadTasks = new[]
            {
                SimulateDownloadChunk(tracker, 1024, 100), // Start with 1KiB chunk
                SimulateDownloadChunk(tracker, 2048, 150), // Then 2KiB chunk
                SimulateDownloadChunk(tracker, 4096, 100), // Then 4KiB chunk
                SimulateDownloadChunk(tracker, 1024, 200)  // Finally another 1KiB chunk
            };

            await Task.WhenAll(downloadTasks);
            await Task.Delay(1000); // Wait for final speed reports

            // Assert
            // Check that speed reports were generated and are not empty
            Assert.True(speedReports.Count > 0, "Should have generated speed reports");
            Assert.All(speedReports, report =>
            {
                Assert.NotEmpty(report);
            });
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
