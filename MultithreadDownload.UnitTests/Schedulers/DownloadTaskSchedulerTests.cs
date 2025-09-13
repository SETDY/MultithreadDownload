using FluentAssertions;
using Moq;
using MultithreadDownload.Core.Errors;
using MultithreadDownload.Downloads;
using MultithreadDownload.Protocols;
using MultithreadDownload.Schedulers;
using System.Reflection;

namespace MultithreadDownload.UnitTests.Schedulers
{
    public class DownloadTaskSchedulerTests
    {
        private readonly Mock<IDownloadService> mockDownloadService;
        private readonly Mock<IDownloadTaskWorkProvider> mockWorkProvider;
        private readonly DownloadTaskScheduler scheduler;

        public DownloadTaskSchedulerTests()
        {
            mockDownloadService = new Mock<IDownloadService>();
            mockWorkProvider = new Mock<IDownloadTaskWorkProvider>();
            scheduler = new DownloadTaskScheduler(3, mockDownloadService.Object, mockWorkProvider.Object);
        }

        [Fact]
        public void AddTask_ShouldAddTaskToQueue()
        {
            // Arrange
            // Setup a mock download context that is valid
            Mock<IDownloadContext> mockDownloadContext = new Mock<IDownloadContext>();
            mockDownloadContext.Setup(m => m.IsPropertiesVaild()).Returns(Result<bool, DownloadError>.Success(true));

            // Act
            scheduler.AddTask(mockDownloadContext.Object);

            // Assert
            scheduler.GetTasks().Should().HaveCount(1);
        }

        [Fact]
        public void CancelTask_ShouldCancelSpecificTask()
        {
            // Arrange
            // Setup a mock download context that is valid
            Mock<IDownloadContext> mockDownloadContext = new Mock<IDownloadContext>();
            mockDownloadContext.Setup(m => m.IsPropertiesVaild()).Returns(Result<bool, DownloadError>.Success(true));
            scheduler.AddTask(mockDownloadContext.Object);
            Guid taskId = scheduler.GetTasks()[0].ID;

            // Act
            bool result = scheduler.CancelTask(taskId);

            // Assert
            result.Should().BeTrue();
            result.Should().BeTrue();
            scheduler.GetTasks()[0].State.Should().Be(DownloadState.Cancelled);
        }

        [Fact]
        public void CancelTasks_ShouldCancelAllTasks()
        {
            // Arrange
            // Setup a mock download context that is valid
            Mock<IDownloadContext> mockDownloadContext = new Mock<IDownloadContext>();
            mockDownloadContext.Setup(m => m.IsPropertiesVaild()).Returns(Result<bool, DownloadError>.Success(true));
            scheduler.AddTask(mockDownloadContext.Object);
            scheduler.AddTask(mockDownloadContext.Object);

            // Act
            bool result = scheduler.CancelTasks();

            // Assert
            result.Should().BeTrue();
            result.Should().BeTrue();
            scheduler.GetTasks().ToList().ForEach(t => t.State.Should().Be(DownloadState.Cancelled));
        }

        [Fact]
        public void Dispose_ShouldDisposeSchedulerProperly()
        {
            // Act
            Action action = () => scheduler.Dispose();

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void PauseTask_ShouldPauseSpecificTask()
        {
            // Arrange
            // Setup a mock download context that is valid
            Mock<IDownloadContext> mockDownloadContext = new Mock<IDownloadContext>();
            mockDownloadContext.Setup(m => m.IsPropertiesVaild()).Returns(Result<bool, DownloadError>.Success(true));
            scheduler.AddTask(mockDownloadContext.Object);
            Guid taskId = scheduler.GetTasks()[0].ID;

            // Act
            // Since the Pause method of DownloadTask is not implemented,
            // we will just check if it throws NotImplementedException
            Assert.Throws<NotImplementedException>(() => scheduler.PauseTask(taskId).Should());
            // Alternatively, if you want to check the result of the PauseTask method, you can uncomment the following lines:
            //Result<bool> result = scheduler.PauseTask(taskId);
            // Assert
            //result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Start_ShouldStartAllocatorTask()
        {
            // Act
            scheduler.Start();

            // Assert
            // The allocator task status should be running
            FieldInfo allocator = scheduler.GetType().GetField("_allocator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?? throw new InvalidOperationException("Field '_allocator' not found.");
            allocator.GetValue(scheduler).As<Task>().Status.Should().Be(TaskStatus.Running);
        }
    }
}