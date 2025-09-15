using MultithreadDownload.Events;
using MultithreadDownload.Logging;
using MultithreadDownload.Protocols;
using MultithreadDownload.Schedulers;
using MultithreadDownload.Tasks;
using System;
using System.Linq;

namespace MultithreadDownload.Core
{
    /// <summary>
    /// The MultiDownload class is responsible for managing multiple download tasks by using a single type of download service etc.
    /// </summary>
    public sealed class MultiDownload : IDisposable
    {
        #region Private Fields
        /// <summary>
        /// The scheduler that manages the download tasks.
        /// </summary>
        private readonly IDownloadTaskScheduler _taskScheduler;

        /// <summary>
        /// The download service used to perform the downloads.
        /// </summary>
        private readonly IDownloadService _downloadService;

        /// <summary>
        /// The maximum number of parallel tasks that can be executed at the same time.
        /// </summary>
        private readonly byte _maxParallelTasks;

        /// <summary>
        /// The work provider used to manage the download tasks.
        /// </summary>
        private readonly IDownloadTaskWorkProvider _workProvider;
        #endregion

        #region Events
        /// <summary>
        /// Event raised when the progress of the task queue changes.
        /// </summary>
        public event EventHandler<DownloadDataEventArgs> TaskQueueProgressChanged;

        /// <summary>
        /// Event raised when a task have completed its progress.
        /// </summary>
        public event EventHandler TasksProgressCompleted;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiDownload"/> class.
        /// </summary>
        /// <param name="maxParallelTasks">The maximum number of parallel downloading tasks.</param>
        /// <param name="downloadService">The download service to use.</param>
        /// <param name="workProvider">The work provider to use.</param>
        /// <remarks>
        /// This type of initialization is used when the user wants to use a custom download settings (e.g. custom service and work provider)
        /// </remarks>
        public MultiDownload(byte maxParallelTasks, IDownloadService downloadService, IDownloadTaskWorkProvider workProvider)
        {
            /// Validate the parameters
            ValidateParameters(downloadService, workProvider);

            // Initialize the properties
            this._downloadService = downloadService;
            this._maxParallelTasks = maxParallelTasks;
            this._workProvider = workProvider;
            this._taskScheduler = new DownloadTaskScheduler(_maxParallelTasks, _downloadService, _workProvider);

            // Log the initialization
            DownloadLogger.LogInfo($"MultiDownload initialized with {downloadService.GetType().Name} and maxParallelTasks = {_maxParallelTasks}");

            // Hook up the events
            HookEvents();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiDownload"/> class.
        /// </summary>
        /// <param name="maxParallelTasks">The maximum number of parallel downloading tasks.</param>
        /// <param name="serviceType">The type of download service to use.</param>
        /// <remarks>
        /// This type of initialization is used when the user wants to use the default download settings (e.g. default service and work provider)
        /// </remarks>
        public MultiDownload(byte maxParallelTasks, DownloadServiceType serviceType) : this(maxParallelTasks, DownloadServiceFactory.CreateService(serviceType), new DownloadTaskWorkProvider())
        { }

        #region Allocator Methods

        /// <summary>
        /// Starts the download task scheduler.
        /// </summary>
        public void StartAllocator() => this._taskScheduler.Start();

        /// <summary>
        /// Stops the download task scheduler.
        /// </summary>
        public void StopAllocator() => this._taskScheduler.Stop();

        #endregion Allocator Methods

        #region Task Management Methods
        /// <summary>
        /// Gets all download tasks managed by the scheduler.
        /// </summary>
        /// <returns>The array of all download tasks.</returns>
        public DownloadTask[] GetDownloadTasks() => this._taskScheduler.GetTasks();

        /// <summary>
        /// Gets download tasks that match the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to filter tasks.</param>
        /// <returns>The array of download tasks that match the predicate.</returns>
        public DownloadTask[] GetDownloadTasks(Func<DownloadTask,bool> predicate)
        {
            return _taskScheduler.GetTasks().Where(predicate).ToArray();
        }

        /// <summary>
        /// Add a task to the download queue.
        /// </summary>
        /// <param name="downloadContext">Download context.</param>
        public DownloadTask AddTask(IDownloadContext downloadContext)
        {
            // Validate the download context
            // First, check if the download context is null
            // Then, check if the properties of the download context are valid
            // Finally, check if the download context matches the download service
            ArgumentNullException.ThrowIfNull(downloadContext, "The download context cannot be null.");
            if (!downloadContext.IsPropertiesVaild().Value.UnwrapOr(false))
                throw new ArgumentException("The download context is not valid.");
            if (!_downloadService.IsSupportedDownloadContext(downloadContext))
                throw new ArgumentException("The download context is not supported by the download service.");

            // Add the task to the scheduler
            DownloadTask addedTask = _taskScheduler.AddTask(downloadContext);
            // Log the addition of the task
            DownloadLogger.LogInfo($"The Task is added with id: {addedTask.ID} and path: {downloadContext.TargetPath}");
            // Return the added task
            return addedTask;
        }

        /// <summary>
        /// Pause a download tasks.
        /// </summary>
        /// <param name="taskId">The ID of the task to pause.</param>
        public void PauseTask(Guid taskId) => this._taskScheduler.PauseTask(taskId);

        /// <summary>
        /// Resume a download tasks.
        /// </summary>
        /// <param name="taskId">The ID of the task to resume.</param>
        public void ResumeTask(Guid taskId) => this._taskScheduler.ResumeTask(taskId);

        /// <summary>
        /// Cancel a download tasks.
        /// </summary>
        /// <param name="taskId">The ID of the task to cancel.</param>
        public void Cancel(Guid taskId) => this._taskScheduler.CancelTask(taskId);

        #endregion Task Management Methods

        /// <summary>
        /// Dispose of the MultiDownload instance and release resources.
        /// </summary>
        public void Dispose()
        {
            this._taskScheduler.Dispose();
        }

        #region Private Methods

        /// <summary>
        /// Validates the parameters passed to the constructor.
        /// </summary>
        /// <param name="parameters">The parameters to validate.</param>
        private void ValidateParameters(params object[] parameters)
        {
            // Check if any of the parameters are null
            foreach (object parameter in parameters)
            {
                ArgumentNullException.ThrowIfNull(parameter, "The parameter cannot be null.");
            }
        }

        /// <summary>
        /// Subscribes to progress-related events from the internal task scheduler and relays them to external event
        /// handlers.
        /// </summary>
        /// <remarks>This method enables forwarding of task progress notifications by attaching internal
        /// event handlers. It should be called before external consumers subscribe to related events to ensure
        /// notifications are received.</remarks>
        private void HookEvents()
        {
            this._taskScheduler.TaskQueueProgressChanged += (s, e) => TaskQueueProgressChanged?.Invoke(this, e);
            this._taskScheduler.TasksProgressCompleted += (s, e) => TasksProgressCompleted?.Invoke(this, e);
        }

        #endregion Private Methods
    }
}