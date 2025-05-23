using FluentAssertions;
using MultithreadDownload.Tasks;
using MultithreadDownload.UnitTests.Fixtures;

namespace MultithreadDownload.UnitTests.Tasks
{
    public class DownloadSpeedMonitorTests
    {
        [Fact]
        public async Task DownloadSpeedMonitor_ShouldReportLowSpeed()
        {
            // Arrange
            long downloadedBytes = 0;
            var monitor = new DownloadSpeedMonitor();
            var tcs = new TaskCompletionSource<string>();

            monitor.OnSpeedUpdated += speed => tcs.TrySetResult(speed);
            monitor.Start(() => downloadedBytes);

            // Simulate 512 bytes download and wait about 1 second
            downloadedBytes += 512;
            await Task.Delay(1000);

            // Assert
            string speedStr = await tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(3));
            speedStr.Should().Contain("B/s");
            // Since the timer is not precise, we need to check the range
            int speedValue = int.Parse(speedStr.Split(' ')[0]);
            speedValue.Should().BeInRange(490, 520);

            monitor.Stop();
        }

        [Fact]
        public async Task DownloadSpeedMonitor_ShouldReportHighSpeed()
        {
            // Arrange
            long downloadedBytes = 0;
            var monitor = new DownloadSpeedMonitor();
            var tcs = new TaskCompletionSource<string>();

            monitor.OnSpeedUpdated += speed => tcs.TrySetResult(speed);
            monitor.Start(() => downloadedBytes);

            // Simulate 5 MiB download and wait about 1 second
            downloadedBytes += 5 * 1024 * 1024;
            await Task.Delay(1100);

            // Wait for the next tick before asserting
            string? speedStr = await tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(3));

            speedStr.Should().Contain("MiB/s");
            // Since the timer is not precise, we need to check the range
            double speedValue = double.Parse(speedStr.Split(' ')[0]);
            speedValue.Should().BeInRange(4.8, 5.2);

            monitor.Stop();
        }


        [Fact]
        public async Task DownloadSpeedMonitor_ShouldReportContinuousGrowth()
        {
            // Arrange
            long downloadedBytes = 0;
            var monitor = new DownloadSpeedMonitor();
            var tcsSecond = new TaskCompletionSource<string>();
            var tcsThird = new TaskCompletionSource<string>();

            int count = 0;
            monitor.OnSpeedUpdated += speed =>
            {
                count++;
                if (count == 2)
                {
                    tcsSecond.TrySetResult(speed);
                }
                else if (count == 3)
                {
                    tcsThird.TrySetResult(speed);
                }
            };

            monitor.Start(() => downloadedBytes);

            // Tick 1: no change
            // Tick 2: +1 KiB (Add 1100 to prevent a error)
            await Task.Delay(1100);
            downloadedBytes += 1100;

            // Tick 3: +2 KiB (Add 2100 to prevent a error)
            await Task.Delay(1100);
            downloadedBytes += 2100;

            // Assert Tick 2
            var speedStr2 = await tcsSecond.Task.TimeoutAfter(TimeSpan.FromSeconds(3));
            speedStr2.Should().Contain("KiB/s").And.Contain("1");

            // Assert Tick 3
            var speedStr3 = await tcsThird.Task.TimeoutAfter(TimeSpan.FromSeconds(3));
            speedStr3.Should().Contain("KiB/s").And.Contain("2");

            monitor.Stop();
        }

        [Fact]        
        public async Task DownloadSpeedMonitor_ShouldNotRaiseEventAfterStop()
        {
            // Arrange
            long downloadedBytes = 0;
            var monitor = new DownloadSpeedMonitor();
            bool eventRaised = false;

            monitor.Start(() => downloadedBytes);

            monitor.OnSpeedUpdated += speed =>
            {
                eventRaised = true;
            };

            // Stop the monitor immediately
            monitor.Stop();

            // Simulate waiting (no event should fire)
            await Task.Delay(1200);

            eventRaised.Should().BeFalse("no SpeedUpdated event should fire after stopping");
        }


        [Fact]
        public async Task DownloadSpeedMonitor_ShouldHandleZeroGrowth()
        {
            // Arrange
            long downloadedBytes = 0;
            var monitor = new DownloadSpeedMonitor();
            var tcs = new TaskCompletionSource<string>();

            monitor.Start(() => downloadedBytes);

            monitor.OnSpeedUpdated += speed =>
            {
                tcs.TrySetResult(speed);
            };

            // Wait for the first tick (zero growth)
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));

            completed.Should().Be(tcs.Task, because: "SpeedUpdated event should fire even if no data downloaded");

            string speedStr = await tcs.Task;
            speedStr.Should().Be("0 B/s");

            // Cleanup
            monitor.Stop();
        }
    }
}
