using MultithreadDownload.Events;
using MultithreadDownload.Logging;
using MultithreadDownload.Protocols;
using MultithreadDownload.Schedulers;
using MultithreadDownload.Tasks;
using System;

namespace MultithreadDownload.Core
{
    /// <summary>
    /// The MultiDownload class is responsible for managing multiple download tasks.
    /// </summary>
    public class MultiDownload : IDisposable
    {
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

        /// <summary>
        /// Event raised when the progress of the task queue changes.
        /// </summary>
        public event EventHandler<DownloadDataEventArgs> TaskQueueProgressChanged;

        /// <summary>
        /// Event raised when a task have completed its progress.
        /// </summary>
        public event EventHandler TasksProgressCompleted;

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
            ValidateParameters(downloadService);

            // Initialize the properties
            _downloadService = downloadService;
            _maxParallelTasks = maxParallelTasks;
            _workProvider = workProvider;
            _taskScheduler = new DownloadTaskScheduler(_maxParallelTasks, _downloadService, _workProvider);

            // Log the initialization
            DownloadLogger.LogInfo($"MultiDownload initialized with {downloadService.GetType().Name} and maxParallelTasks = {_maxParallelTasks}");

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
        public MultiDownload(byte maxParallelTasks, DownloadServiceType serviceType)
        {
            // Initialize the properties
            _downloadService = DownloadServiceFactory.CreateService(serviceType);
            _maxParallelTasks = maxParallelTasks;
            _workProvider = new DownloadTaskWorkProvider();
            _taskScheduler = new DownloadTaskScheduler(_maxParallelTasks, _downloadService, _workProvider);

            // Log the initialization
            DownloadLogger.LogInfo($"MultiDownload initialized with {serviceType} and maxParallelTasks = {_maxParallelTasks}");

            HookEvents();
        }

        #region Private Methods

        /// <summary>
        /// Validates the parameters passed to the constructor.
        /// </summary>
        /// <param name="parameters">The parameters to validate.</param>
        private void ValidateParameters(params object[] parameters)
        {
            foreach (object parameter in parameters)
            {
                ArgumentNullException.ThrowIfNull(parameter);
            }
        }

        private void HookEvents()
        {
            _taskScheduler.TaskQueueProgressChanged += (s, e) => TaskQueueProgressChanged?.Invoke(this, e);
            _taskScheduler.TasksProgressCompleted += (s, e) => TasksProgressCompleted?.Invoke(this, EventArgs.Empty);
        }

        #endregion Private Methods

        #region Allocator Methods

        /// <summary>
        /// Starts the download task scheduler.
        /// </summary>
        public void StartAllocator()
        {
            _taskScheduler.Start();
        }

        /// <summary>
        /// Stops the download task scheduler.
        /// </summary>
        public void StopAllocator()
        {
            _taskScheduler.Stop();
        }

        #endregion Allocator Methods

        #region Task Management Methods

        public DownloadTask[] GetDownloadTasks()
        {
            return _taskScheduler.GetTasks();
        }

        /// <summary>
        /// Add a task to the download queue.
        /// </summary>
        /// <param name="downloadContext">Download context.</param>
        public DownloadTask AddTask(IDownloadContext downloadContext)
        {
            if (downloadContext == null)
            {
                throw new ArgumentNullException(nameof(downloadContext));
            }
            // TODO: Validate the download context whether it matches the download service
            DownloadTask addedTask = _taskScheduler.AddTask(downloadContext);

            // Log the addition of the task
            DownloadLogger.LogInfo($"The Task is added with id: {addedTask.ID} and path: {downloadContext.TargetPath}");

            return addedTask;
        }

        /// <summary>
        /// Pause a download tasks.
        /// </summary>
        /// <param name="taskId">The ID of the task to pause.</param>
        public void PauseTask(Guid taskId) => _taskScheduler.PauseTask(taskId);

        /// <summary>
        /// Resume a download tasks.
        /// </summary>
        /// <param name="taskId">The ID of the task to resume.</param>
        public void ResumeTask(Guid taskId) => _taskScheduler.ResumeTask(taskId);

        /// <summary>
        /// Cancel a download tasks.
        /// </summary>
        /// <param name="taskId">The ID of the task to cancel.</param>
        public void Cancel(Guid taskId) => _taskScheduler.CancelTask(taskId);

        #endregion Task Management Methods

        /// <summary>
        /// Dispose of the MultiDownload instance and release resources.
        /// </summary>
        public void Dispose()
        {
            _taskScheduler.Dispose();
        }
    }
}