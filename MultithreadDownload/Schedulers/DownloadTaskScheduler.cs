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
    public class DownloadTaskScheduler : IDownloadTaskScheduler, IDisposable
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

        private readonly IDownloadService s_downloadService;

        /// <summary>
        /// The provider that provides the work for the download tasks.
        /// </summary>
        private readonly IDownloadTaskWorkProvider s_workProvider;

        /// <summary>
        /// The number of maximum concurrent downloads.
        /// </summary>
        public byte MaxParallelTasks { get; private set; }

        /// <summary>
        /// The event will be invoked when the progress of a task changes.
        /// which means the task is downloaded newly and the task is added to the queue.
        /// </summary>
        public event EventHandler<DownloadDataEventArgs> TaskQueueProgressChanged;

        /// <summary>
        /// The event will be invoked when a task is completed its progress.
        /// </summary>
        /// <remarks>
        /// The progress of the task means it completes its progress but not the downloading is completed successfully.
        /// </remarks>
        public event EventHandler<DownloadDataEventArgs> TasksProgressCompleted;

        public DownloadTaskScheduler(byte maxDownloadingTasks, IDownloadService downloadService, IDownloadTaskWorkProvider workProvider)
        {
            // Initialize the properties
            this.MaxParallelTasks = maxDownloadingTasks;
            s_downloadSlots = new SemaphoreSlim(this.MaxParallelTasks);
            s_workProvider = workProvider;
            s_downloadService = downloadService;

            // Initialize the allocator task
            // If there are no tasks in the queue, the allocator task will wait for a task to be added.
            // If there are enough slots available, the allocator task will start the task.
            // The process will continue until the software is stopped.
            this.s_allocator = new Task(() =>
            {
                foreach (DownloadTask task in s_taskQueue.GetConsumingEnumerable())
                {
                    // The download slots -1 if there is available solt and force start the task
                    // Otherwise, block the task to wait
                    this.s_downloadSlots.Wait();
                    task.Start(s_workProvider, s_downloadService);
                }
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Dispose the task scheduler.
        /// </summary>
        /// <remarks>
        /// Since the task scheduler manages the donwload tasks, 
        /// these tasks which were managered by the task scheduler will be cancelled too when the scheduler has been dispose.
        /// </remarks>
        public void Dispose()
        {
            // Cancel all tasks in the queue
            // Dispose the allocator, the task queue and the download slots
            this.GetTasks().ToList().ForEach(task => task.Cancel());
            s_allocator.Dispose();
            s_taskQueue?.Dispose();
            s_downloadSlots?.Dispose();
        }
        #region Methods of task allocator

        /// <summary>
        /// Start the queue process of the tasks(allocator).
        /// </summary>
        /// <returns>Whether the allocator are paused successfully.</returns>
        /// <remarks>
        /// It will only start the allocator task but not the tasks in the queue.
        /// </remarks>
        public Result<bool> Start()
        {
            if (s_allocator.Status != TaskStatus.Running) { Result<bool>.Failure("The allocator is already started."); }
            s_allocator.Start();
            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Pause the queue process of the tasks.
        /// </summary>
        /// <returns>Whether the allocator are paused successfully.</returns>
        /// <remarks>
        /// It will only start the allocator task but not the tasks in the queue.
        /// </remarks>
        public Result<bool> Pause()
        {
            // Pause the allocator task
            // The tasks in the queue will be paused
            if (s_allocator.Status != TaskStatus.WaitingForActivation) { Result<bool>.Failure("The allocator is already paused."); }

            s_allocator.Wait();
            return Result<bool>.Success(true);
        }
#endregion

        #region Mothods about download task

        /// <summary>
        /// Add a task to the queue.
        /// </summary>
        /// <param name="downloadContext">The download context to use.</param>
        public void AddTask(IDownloadContext downloadContext)
        {
            // Create a new task with the given download context and maximum number of threads
            // Hook the event such that the queue progress can be invoked when the task is completed.
            DownloadTask task = DownloadTask.Create(Guid.NewGuid(), this.MaxParallelTasks, downloadContext);
            task.Completed += delegate
            {
                // The download slots +1
                // Invoke the event when the task is completed
                this.s_downloadSlots.Release();
                this.TasksProgressCompleted.Invoke(task, new DownloadDataEventArgs(task));
            };
            this.s_taskQueue.Add(task);
            this.TaskQueueProgressChanged.Invoke(task, new DownloadDataEventArgs(task));
        }

        /// <summary>
        /// Add a task to the queue.
        /// </summary>
        /// <param name="task">The task to add.</param>
        public void AddTask(DownloadTask task)
        {
            // Hook the event such that the queue progress can be invoked when the task is completed.
            task.Completed += delegate
            {
                // The download slots +1
                // Invoke the event when the task is completed
                this.s_downloadSlots.Release();
                this.TasksProgressCompleted.Invoke(task, new DownloadDataEventArgs(task));
            };
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
        /// Pause a task that is in the queue.
        /// </summary>
        /// <param name="taskId">The id of the task to pause.</param>
        public Result<bool> PauseTask(Guid taskID)
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
        public Result<bool> ResumeTask(Guid taskID)
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
        public Result<bool> CancelTask(Guid taskID)
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
        public Result<bool> CancelTasks()
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
        #endregion
    }
}
