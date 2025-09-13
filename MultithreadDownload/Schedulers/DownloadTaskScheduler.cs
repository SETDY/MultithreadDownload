using MultithreadDownload.Downloads;
using MultithreadDownload.Events;
using MultithreadDownload.Logging;
using MultithreadDownload.Protocols;
using MultithreadDownload.Tasks;
using MultithreadDownload.Primitives;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MultithreadDownload.Core.Errors;
using MultithreadDownload.Utils;

namespace MultithreadDownload.Schedulers
{
    public class DownloadTaskScheduler : IDownloadTaskScheduler, IDisposable
    {
        /// <summary>
        /// The map of all the tasks including the tasks that are not in the queue. e.g. the tasks that are completed or cancelled.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, DownloadTask> _taskMap = new();

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
        private Task _allocator;

        /// <summary>
        /// The lock that is used to synchronize access to the allocator.
        /// </summary>
        private readonly object _allocatorLock = new object();

        /// <summary>
        /// Whether the allocator is running.
        /// </summary>
        private bool isAllocatorRunning = false;

        /// <summary>
        /// The cancellation token source that is used to cancel the allocator task.
        /// </summary>
        private readonly CancellationTokenSource _allocatorTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The download service that is used to download the files.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the DownloadTaskScheduler class with the specified maximum number of parallel
        /// download tasks, download service, and work provider.
        /// </summary>
        /// <remarks>
        /// The scheduler begins allocating and executing download tasks when the <see cref="DownloadTaskScheduler.Start"/> method is invoked. 
        /// Tasks are managed according to the specified parallelism limit and are obtained from theprovided work provider. To stop the scheduler, invoke the <see cref="DownloadTaskScheduler.Stop"/> method.
        /// </remarks>
        /// <param name="maxDownloadingTasks">The maximum number of download tasks that can run in parallel. Must be greater than 0.</param>
        /// <param name="downloadService">The service used to perform download operations for each scheduled task.</param>
        /// <param name="workProvider">The provider that supplies download tasks to be scheduled and executed.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxDownloadingTasks"/> is less than or equal to 0.</exception>
        public DownloadTaskScheduler(byte maxDownloadingTasks, IDownloadService downloadService, IDownloadTaskWorkProvider workProvider)
        {
            // Check the parameters
            if (maxDownloadingTasks <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxDownloadingTasks), "The maximum number of parallel tasks must be greater than 0.");
            // Initialize the properties
            this.MaxParallelTasks = maxDownloadingTasks;
            _downloadSlots = new SemaphoreSlim(this.MaxParallelTasks);
            _workProvider = workProvider;
            _downloadService = downloadService;

            // Initialize the allocator task
            // If there are no tasks in the queue, the allocator task will wait for a task to be added.
            // If there are enough slots available, the allocator task will start the task.
            // The process will continue until the software is stopped or the client invokes the Stop method.
            this.Reset();
        }

        /// <summary>
        /// Allocate tasks from the queue to start downloading.
        /// </summary>
        private void AllocateTasks()
        {
            // Continuously allocate tasks from the queue
            // If there are no tasks in the queue, the method will block until a task is added.
            // If there are enough slots available, the method will start the task.
            // If it is failed to allocate a task, it will retry 5 times with a delay of 1.5 seconds between each retry.
            foreach (DownloadTask task in _taskQueue.GetConsumingEnumerable(_allocatorTokenSource.Token))
            {
                RetryHelper.Retry(
                    5, 
                    1500, 
                    () => AllocateTask(task), 
                    () => DownloadError.Create(DownloadErrorCode.UnexpectedOrUnknownException, $"Unexpected error when allocating the task with ID {task.ID}.")
                );
            }
        }

        /// <summary>
        /// Allocates a download slot and initiates execution of the specified download task.
        /// </summary>
        /// <param name="task">The download task to be started. Cannot be null.</param>
        private Result<bool, DownloadError> AllocateTask(DownloadTask task)
        {
            // Check if the task is null
            // If it is null, return failure result
            // Otherwise, allocate a download slot and start the task
            if (task == null)
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.NullReference, "The task cannot be null."));
            try
            {
                // The download slots -1 if there is available solt and force start the task
                // Otherwise, block the task to wait
                this._downloadSlots.Wait(_allocatorTokenSource.Token);
                // Log the start of the task
                //DownloadLogger.LogInfo($"The Task with id: {task.ID} and path: {task.DownloadContext.TargetPath} has been started.");
                task.ExecuteDownloadTask(_workProvider, _downloadService);
                // Return success result
                return Result<bool, DownloadError>.Success(true);
            }
            catch (Exception)
            {
                // If there is an exception, log the error and continue to the next task
                DownloadLogger.For(Option<string>.Some(task.ID.ToString()), Option<byte>.None()).LogError($"Unexpected error when starting the task with path: {task.DownloadContext.TargetPath}.");
                // Return failure result
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.UnexpectedOrUnknownException, $"Unexpected error when starting the task with ID {task.ID.ToString()}."));
            }
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
            // Stop and dispose the task queue and the download slots
            this.GetTasks().ToList().ForEach(task => task.Cancel());
            Stop();
            _taskQueue?.Dispose();
            _downloadSlots?.Dispose();
        }

        #region Methods of task allocator

        /// <summary>
        /// Reset the allocator.
        /// </summary>
        public void Reset()
        {
            this._allocator = new Task(() => this.AllocateTasks(), TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Start the allocator.
        /// </summary>
        /// <remarks>
        /// It will only start the allocator but not the tasks in the queue.
        /// </remarks>
        public void Start()
        {
            // Check the status of the allocator task
            // If it is already running, throw an exception
            // Otherwise, start the allocator task
            if (this.isAllocatorRunning)
                throw new InvalidOperationException("The allocator is already started.");
            if (this._allocator.IsCanceled)
                throw new InvalidOperationException("The allocator has been cancelled and cannot be restarted. Please create a new instance of the scheduler.");
            // Start the allocator task
            this._allocator.Start();
        }

        /// <summary>
        /// Stop the allocator.
        /// </summary>
        /// <remarks>
        /// It will stop the allocator but not all tasks in the queue.
        /// </remarks>
        public void Stop()
        {
            // Cancel the allocator task 
            // Using lock to prevent multiple threads from stopping the allocator at the same time
            // In particular, there is no race condition when checking and setting isAllocatorRunning due to the design of the method.
            // However, to ensure thread safety and prevent potential issues in future modifications, a lock is used here.
            lock (this._allocatorLock)
            {
                if (!this.isAllocatorRunning)
                    throw new InvalidOperationException("The allocator is already stopped.");
                this.isAllocatorRunning = false;
            }
            // Cancel the allocator token
            this._allocatorTokenSource.Cancel();
            // Wait for the allocator task to complete
            try
            {
                // Wait for a maximum of 5 seconds for the allocator to finish
                bool finished = _allocator.Wait(TimeSpan.FromMilliseconds(5000));
                // If the allocator did not finish within the timeout, throw a timeout exception
                if (!finished)
                    throw new TimeoutException("The allocator did not stop within timeout.");
            }
            catch (AggregateException ex)
            {
                // Task.Wait throws AggregateException; typically contains OperationCanceledException
                ex.Handle(inner => inner is OperationCanceledException);
            }
            finally
            {
                try 
                {
                    // Dispose the allocator if it is completed
                    if (_allocator != null && _allocator.IsCompleted) 
                        _allocator.Dispose(); 
                } 
                catch 
                {
                    // Log the error if there is an exception when disposing the allocator
                    DownloadLogger.LogError("Unexpected error when disposing the allocator task.");
                }
            }
        }

        #endregion Methods of task allocator

        #region Mothods about download task

        /// <summary>
        /// Add a task to the queue.
        /// </summary>
        /// <param name="downloadContext">The download context to use.</param>
        /// <returns>The task that is added to the queue.</returns>
        public DownloadTask AddTask(IDownloadContext downloadContext)
        {
            // Create a new task with the given download context and maximum number of threads
            // Hook the event such that the queue progress can be invoked when the task is completed.
            DownloadTask task = DownloadTask.Create(Guid.NewGuid(), downloadContext);

            // Log the creation of the task
            //DownloadLogger.LogInfo($"A Task is created with id: {task.ID} and path: {downloadContext.TargetPath}");

            task.TaskCompleted += delegate
            {
                // The download slots +1
                // Invoke the event when the task is completed
                this._downloadSlots.Release();

                // Add an ? to prevent the event is invoked when it is null.
                this.TasksProgressCompleted?.Invoke(task, new DownloadDataEventArgs(task));
            };
            // Add the task to the task map and the task queue
            // If the task already exists in the task map, throw an exception
            // Because it is impossible to have two tasks with the same ID by using Guid.NewGuid()
            bool result = _taskMap.TryAdd(task.ID, task);
            if (!result)
                throw new InvalidOperationException($"Unexpected exception: The task with id {task.ID} already exists in the task map.");
            _taskQueue.Add(task);

            // Log the additon of the queued task
            DownloadLogger.LogInfo($"The Task is queued with id: {task.ID} and path: {downloadContext.TargetPath}");

            // Add an ? to prevent the event is invoked when it is null.
            TaskQueueProgressChanged?.Invoke(task, new DownloadDataEventArgs(task));

            return task;
        }

        /// <summary>
        /// Add a task to the queue.
        /// </summary>
        /// <param name="task">The task to add.</param>
        public void AddTask(DownloadTask task)
        {
            // Hook the event such that the queue progress can be invoked when the task is completed.
            task.TaskCompleted += delegate
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
        /// <returns>All tasks in the map.</returns>
        public DownloadTask[] GetTasks()
        {
            // Get all the tasks in the map and return them as an array.
            return _taskMap.ToList().Select(x => x.Value).ToArray();
        }

        /// <summary>
        /// Get tasks that are in the queue.
        /// </summary>
        /// <returns>The tasks that satisfy the condition in the map.</returns> 
        public DownloadTask[] GetTasks(DownloadState state)
        {
            // Get the tasks that are satisfied the condition in the map and return them as an array.
            return _taskMap.ToList().Where(x => x.Value.State == state).Select(x => x.Value).ToArray();
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
                if (task.ID == taskID && task.State != DownloadState.Cancelled && task.State != DownloadState.Completed)
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
                if (task.ID == taskID && task.State == DownloadState.Paused)
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