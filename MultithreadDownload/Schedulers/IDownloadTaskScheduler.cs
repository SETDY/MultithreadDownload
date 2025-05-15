using MultithreadDownload.Downloads;
using MultithreadDownload.Events;
using MultithreadDownload.Protocols;
using MultithreadDownload.Tasks;
using MultithreadDownload.Utils;
using System;

namespace MultithreadDownload.Schedulers
{
    public interface IDownloadTaskScheduler : IDisposable
    {
        /// <summary>
        /// The event will be invoked when the progress of a task changes.
        /// which means the task is downloaded newly or the task is added to the queue.
        /// </summary>
        public event EventHandler<DownloadDataEventArgs> TaskQueueProgressChanged;

        /// <summary>
        /// The event will be invoked when a task is completed its progress.
        /// </summary>
        /// <remarks>
        /// The progress of the task means it completes its progress but not the downloading is completed successfully.
        /// </remarks>
        public event EventHandler<DownloadDataEventArgs> TasksProgressCompleted;

        /// <summary>
        /// Start the queue process of the tasks(allocator).
        /// </summary>
        /// <returns>Whether the allocator are paused successfully.</returns>
        /// <remarks>
        /// It will only start the allocator task but not the tasks in the queue.
        /// </remarks>
        public Result<bool> Start();

        /// <summary>
        /// Stop(Cancel) the queue process of the tasks.
        /// </summary>
        /// <returns>Whether the allocator are paused successfully.</returns>
        /// <remarks>
        /// It will cancel the allocator task but not all tasks in the queue.
        /// </remarks>
        public Result<bool> Stop();

        /// <summary>
        /// Add a task to the queue.
        /// </summary>
        /// <param name="task">The task to add.</param>
        void AddTask(DownloadTask task);

        /// <summary>
        /// Add a task to the queue.
        /// </summary>
        /// <param name="downloadContext">The download context to use.</param>
        DownloadTask AddTask(IDownloadContext downloadContext);


        /// <summary>
        /// Pause a task that is in the queue.
        /// </summary>
        /// <param name="taskId">The id of the task to pause.</param>
        Result<bool> PauseTask(Guid taskId);

        /// <summary>
        /// Resume a task that is in the queue.
        /// </summary>
        /// <param name="taskId">The id of the task to resume.</param>
        Result<bool> ResumeTask(Guid taskId);

        /// <summary>
        /// Cancel a task that is in the queue.
        /// </summary>
        /// <param name="taskId">The id of the task to cancel.</param>
        Result<bool> CancelTask(Guid taskId);

        /// <summary>
        /// Cancel all tasks that is in the queue.
        /// </summary>
        /// <param name="taskId">The id of the task to cancel.</param>
        Result<bool> CancelTasks();

        /// <summary>
        /// Get tasks that are in the queue.
        /// </summary>
        /// <returns>All tasks in the map.</returns>
        public DownloadTask[] GetTasks();

        /// <summary>
        /// Get tasks that are in the queue.
        /// </summary>
        /// <returns>The tasks that satisfy the condition in the map.</returns> 
        public DownloadTask[] GetTasks(DownloadState state);
    }
}