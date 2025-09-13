using MultithreadDownload.Downloads;
using MultithreadDownload.Events;
using MultithreadDownload.Protocols;
using MultithreadDownload.Tasks;
using MultithreadDownload.Primitives;
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
        /// Reset the allocator.
        /// </summary>
        public void Reset();

        /// <summary>
        /// Start the allocator.
        /// </summary>
        /// <remarks>
        /// It will only start the allocator but not the tasks in the queue.
        /// </remarks>
        public void Start();

        /// <summary>
        /// Stop the allocator.
        /// </summary>
        /// <remarks>
        /// It will stop the allocator but not all tasks in the queue.
        /// </remarks>
        public void Stop();

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
        bool PauseTask(Guid taskId);

        /// <summary>
        /// Resume a task that is in the queue.
        /// </summary>
        /// <param name="taskId">The id of the task to resume.</param>
        bool ResumeTask(Guid taskId);

        /// <summary>
        /// Cancel a task that is in the queue.
        /// </summary>
        /// <param name="taskId">The id of the task to cancel.</param>
        bool CancelTask(Guid taskId);

        /// <summary>
        /// Cancel all tasks that is in the queue.
        /// </summary>
        /// <param name="taskId">The id of the task to cancel.</param>
        bool CancelTasks();

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