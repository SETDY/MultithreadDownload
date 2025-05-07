using MultithreadDownload.Downloads;
using MultithreadDownload.Events;
using MultithreadDownload.Protocols;
using MultithreadDownload.Tasks;
using MultithreadDownload.Utils;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Schedulers
{
    public class DownloadTaskScheduler : IDownloadTaskScheduler, IDisposable
    {
        /// <summary>
        /// The queue of tasks to be executed.
        /// </summary>
        private readonly BlockingCollection<DownloadTask> _taskQueue = new BlockingCollection<DownloadTask>();

        /// <summary>
        /// The semaphore that limits the number of concurrent downloads.
        /// </summary>
        /// <remarks>
        /// To prevent deadlocks, the semaphore is initialized with the maximum number of concurrent downloads.
        /// </remarks>
        private readonly SemaphoreSlim _downloadSlots;

        /// <summary>
        /// The task that allocates the download slots.
        /// </summary>
        private readonly Task _allocator;

        private readonly CancellationTokenSource _allocatorTokenSource = new CancellationTokenSource();

        private readonly IDownloadService _downloadService;

        /// <summary>
        /// The provider that provides the work for the download tasks.
        /// </summary>
        private readonly IDownloadTaskWorkProvider _workProvider;

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
            _downloadSlots = new SemaphoreSlim(this.MaxParallelTasks);
            _workProvider = workProvider;
            _downloadService = downloadService;

            // Initialize the allocator task
            // If there are no tasks in the queue, the allocator task will wait for a task to be added.
            // If there are enough slots available, the allocator task will start the task.
            // The process will continue until the software is stopped or the client invokes the Stop method.
            this._allocator = new Task(() =>
            {
                foreach (DownloadTask task in _taskQueue.GetConsumingEnumerable(_allocatorTokenSource.Token))
                {
                    // The download slots -1 if there is available solt and force start the task
                    // Otherwise, block the task to wait
                    this._downloadSlots.Wait(_allocatorTokenSource.Token);
                    task.Start(_workProvider, _downloadService);
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
            // Stop and dispose the allocator, the task queue and the download slots
            this.GetTasks().ToList().ForEach(task => task.Cancel());
            Stop();
            // Since the allocator cannot be cancel by the token when it has not been started,
            // skip the dispose if the allocator is not started.
            // For the allocator task which does not start, it will be recycled by the GC.
            if (_allocator.Status != TaskStatus.Created)
            {
                _allocator.Dispose();
            }
            _taskQueue?.Dispose();
            _downloadSlots?.Dispose();
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
            //TODO: There is a problem which is the if statement cannot prevent the task from starting again.
            if (_allocator.Status == TaskStatus.Running) { Result<bool>.Failure("The allocator is already started."); }
            _allocator.Start();
            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Stop(Cancel) the queue process of the tasks.
        /// </summary>
        /// <returns>Whether the allocator are paused successfully.</returns>
        /// <remarks>
        /// It will cancel the allocator task but not all tasks in the queue.
        /// </remarks>
        public Result<bool> Stop()
        {
            // Cancel the allocator task
            if (_allocator.Status != TaskStatus.Canceled) { Result<bool>.Failure("The allocator is already stop."); }
            _allocatorTokenSource.Cancel();
            return Result<bool>.Success(true);
        }

        #endregion Methods of task allocator

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
                // Add an if statement to prevent the event is invoked when it is null.
                this._downloadSlots.Release();
                if (this.TasksProgressCompleted != null)
                    this.TasksProgressCompleted.Invoke(task, new DownloadDataEventArgs(task));
            };
            this._taskQueue.Add(task);
            // Add an if statement to prevent the event is invoked when it is null.
            if (this.TaskQueueProgressChanged != null)
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
                try
                {
                    // The download slots +1
                    // Invoke the event when the task is completed
                    // Add an if statement to prevent the event is invoked when it is null.
                    this._downloadSlots.Release();
                    if (this.TasksProgressCompleted != null)
                        this.TasksProgressCompleted.Invoke(task, new DownloadDataEventArgs(task));
                }
                catch (Exception)
                {
                    Debug.WriteLine("Unexpected error when invoking the event of task completed.");
                }
            };
            this._taskQueue.Add(task);
        }

        /// <summary>
        /// Get tasks that are in the queue.
        /// </summary>
        /// <returns>All tasks in the queue.</returns>
        public DownloadTask[] GetTasks()
        {
            return _taskQueue.ToArray();
        }

        /// <summary>
        /// Pause a task that is in the queue.
        /// </summary>
        /// <param name="taskId">The id of the task to pause.</param>
        public Result<bool> PauseTask(Guid taskID)
        {
            // If the task is downloading, pause the task that satisfies taskID
            // Otherwise, return failure result.
            foreach (DownloadTask task in this._taskQueue)
            {
                if (task.ID == taskID && task.State != DownloadTaskState.Cancelled && task.State != DownloadTaskState.Completed)
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
            foreach (DownloadTask task in this._taskQueue)
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
            foreach (DownloadTask task in this._taskQueue)
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
                // isCancelled is used to check whether the tasks are cancelled successfully.
                bool isCancelled = false;
                foreach (DownloadTask task in this.GetTasks())
                {
                    task.Cancel();
                    isCancelled = true;
                }
                return Result<bool>.Success(isCancelled);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure("Cannot cancel all tasks, error message: " + ex);
            }
        }

        #endregion Mothods about download task
    }
}