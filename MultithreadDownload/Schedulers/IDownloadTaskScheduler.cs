using MultithreadDownload.Events;
using MultithreadDownload.Tasks;
using MultithreadDownload.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Schedulers
{
    public interface IDownloadTaskScheduler
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

        void AddTask(DownloadTask task);

        /// <summary>
        /// Start the scheduler to process the tasks in the queue.
        /// </summary>
        void Start();

        /// <summary>
        /// Pause a task that is in the queue.
        /// </summary>
        /// <param name="taskId">The id of the task to pause.</param>
        Result<bool> Pause(Guid taskId);

        /// <summary>
        /// Resume a task that is in the queue.
        /// </summary>
        /// <param name="taskId">The id of the task to resume.</param>
        Result<bool> Resume(Guid taskId);

        /// <summary>
        /// Cancel a task that is in the queue.
        /// </summary>
        /// <param name="taskId">The id of the task to cancel.</param>
        Result<bool> Cancel(Guid taskId);

        /// <summary>
        /// Cancel all tasks that is in the queue.
        /// </summary>
        /// <param name="taskId">The id of the task to cancel.</param>
        Result<bool> CancelAll();

        /// <summary>
        /// Get tasks that are in the queue.
        /// </summary>
        /// <returns></returns>
        DownloadTask[] GetTasks();
    }
}
