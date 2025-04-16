using MultithreadDownload.Core;
using MultithreadDownload.Downloads;
using MultithreadDownload.Events;
using MultithreadDownload.Protocols;
using MultithreadDownload.Tasks;
using MultithreadDownload.Utils;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Schedulers
{
    public class DownloadTaskScheduler : IDownloadTaskScheduler
    {
        /// <summary>
        /// The queue of tasks to be executed.
        /// </summary>
        private readonly BlockingCollection<DownloadTask> s_taskQueue = new BlockingCollection<DownloadTask>();

        /// <summary>
        /// The semaphore that limits the number of concurrent downloads.
        /// </summary>
        /// <remarks>
        /// To prevent deadlocks, the semaphore is initialized with the maximum number of concurrent downloads.
        /// </remarks>
        private readonly SemaphoreSlim s_downloadSlots;

        /// <summary>
        /// The task that allocates the download slots.
        /// </summary>
        private readonly Task s_allocator;

        /// <summary>
        /// The provider that provides the work for the download tasks.
        /// </summary>
        private readonly DownloadTaskWorkProvider s_workProvider;

        /// <summary>
        /// The number of maximum concurrent downloads.
        /// </summary>
        public byte MaxDownloadingTasks { get; private set; }

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

        public DownloadTaskScheduler(byte maxDownloadingTasks, DownloadTaskWorkProvider workProvider)
        {
            // Initialize the properties
            this.MaxDownloadingTasks = maxDownloadingTasks;
            this.s_downloadSlots = new SemaphoreSlim(this.MaxDownloadingTasks);
            this.s_workProvider = workProvider;

            // Initialize the allocator task
            // If there are no tasks in the queue, the allocator task will wait for a task to be added.
            // If there are enough slots available, the allocator task will start the task.
            // The process will continue until the software is stopped.
            this.s_allocator = Task.Factory.StartNew(() =>
            {
                foreach (DownloadTask task in this.s_taskQueue.GetConsumingEnumerable())
                {
                    // The download slots -1 if there is available solt
                    // Otherwise, block the task to wait
                    this.s_downloadSlots.Wait();

                    task.Start(() =>
                    {
                        // The download slots +1
                        // Invoke the event when the task is completed
                        this.s_downloadSlots.Release();
                        this.TasksProgressCompleted.Invoke(task, new DownloadDataEventArgs(task));
                    });
                }
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Add a task to the queue.
        /// </summary>
        /// <param name="task">The task to add.</param>
        public void AddTask(IDownloadContext downloadContext)
        {
            DownloadTask task = DownloadTask.Create(Guid.NewGuid(),
                workProvider.Execute_MainWork,
                downloadContext);
            this.s_taskQueue.Add(task);
        }

        /// <summary>
        /// Get tasks that are in the queue.
        /// </summary>
        /// <returns>All tasks in the queue.</returns>
        public DownloadTask[] GetTasks()
        {
            return s_taskQueue.ToArray();
        }

        /// <summary>
        /// Start the scheduler to process the tasks in the queue.
        /// </summary>
        public void Start()
        {
            foreach (DownloadTask task in this.s_taskQueue)
            {
                Result<Stream> result = this.s_workProvider.GetTaskFinalStream(task.DownloadContext);
                if (!result.IsSuccess)
                {
                    throw new Exception("GetTaskFinalStream failed");
                }
                task.Start(this.s_workProvider.Execute_FinalizeWork(result.Value))
            }
        }

        /// <summary>
        /// Pause a task that is in the queue.
        /// </summary>
        /// <param name="taskId">The id of the task to pause.</param>
        public Result<bool> Pause(Guid taskID)
        {
            // If the task is downloading, pause the task that satisfies taskID
            // Otherwise, return failure result.
            foreach (DownloadTask task in this.s_taskQueue)
            {
                if (task.ID == taskID && task.State == DownloadTaskState.Downloading)
                {
                    task?.Pause();
                    return Result<bool>.Success(true);
                }
            }
            return Result<bool>.Failure($"The task with id {taskID} cannot be paused");
        }

        /// <summary>
        /// Resume a task that is already paused
        /// </summary>
        /// <param name="taskId"></param>
        public Result<bool> Resume(Guid taskID)
        {
            // If the task is downloading, resume the task that satisfies taskID
            // Otherwise, return failure result.
            foreach (DownloadTask task in this.s_taskQueue)
            {
                if (task.ID == taskID && task.State == DownloadTaskState.Paused)
                {
                    task?.Pause();
                    return Result<bool>.Success(true);
                }
            }
            return Result<bool>.Failure($"The task with id {taskID} cannot be resumed");
        }

        /// <summary>
        /// Resume a task that is in the queue.
        /// </summary>
        /// <param name="taskId">The id of the task to resume.</param>
        public Result<bool> Cancel(Guid taskID)
        {
            // If the task is downloading, cancel the task that satisfies taskID
            // Otherwise, return failure result.
            foreach (DownloadTask task in this.s_taskQueue)
            {
                if (task.ID == taskID)
                {
                    task?.Cancel();
                    return Result<bool>.Success(true);
                }
            }
            return Result<bool>.Failure($"The task with id {taskID} cannot be cancelled");
        }

        /// <summary>
        /// Cancel all tasks that is in the queue.
        /// </summary>
        /// <returns>Whether the tasks are cancelled successfully.</returns>
        public Result<bool> CancelAll()
        {
            try
            {
                foreach (DownloadTask task in this.GetTasks())
                {
                    task.Cancel();
                }
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure("Cannot cancel all tasks, error message: " + ex);
            }
        }
    }
}
