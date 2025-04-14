using MultithreadDownload.Events;
using MultithreadDownload.Protocols;
using MultithreadDownload.Schedulers;
using MultithreadDownload.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Core
{
    public class MultiDownload : IDisposable
    {
        private readonly IDownloadTaskScheduler s_scheduler;
        private readonly IDownloadService s_downloadService;
        private readonly byte s_maxParallelTasks;

        public event EventHandler<DownloadDataEventArgs> TaskQueueProgressChanged;
        public event EventHandler TasksProgressCompleted;

        public MultiDownload(byte maxParallelTasks, IDownloadService downloadService)
        {
            /// Validate the parameters
            ValidateParameters(downloadService);

            // Initialize the properties
            this.s_downloadService = downloadService;
            this.s_maxParallelTasks = maxParallelTasks;
            this.s_scheduler = new DownloadTaskScheduler(s_maxParallelTasks);

            HookEvents();
        }

        public MultiDownload(byte maxParallelTasks, DownloadServiceType serviceType)
        {
            // Initialize the properties
            s_downloadService = DownloadServiceFactory.CreateService(serviceType);
            s_maxParallelTasks = maxParallelTasks;
            s_scheduler = new DownloadTaskScheduler(s_maxParallelTasks);

            HookEvents();
        }

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
            s_scheduler.TaskQueueProgressChanged += (s, e) => TaskQueueProgressChanged?.Invoke(this, e);
            s_scheduler.TasksProgressCompleted += (s, e) => TasksProgressCompleted?.Invoke(this, EventArgs.Empty);
        }

        public void AddTask(IDownloadContext downloadContext)
        {

            s_scheduler.AddTask(task);
        }

        /// <summary>
        /// Start the download tasks.
        /// </summary>
        public void Start() => s_scheduler.Start();

        /// <summary>
        /// Pause a download tasks.
        /// </summary>
        /// <param name="taskId">The ID of the task to pause.</param>
        public void Pause(Guid taskId) => s_scheduler.Pause(taskId);

        /// <summary>
        /// Resume a download tasks.
        /// </summary>
        /// <param name="taskId">The ID of the task to resume.</param>
        public void Resume(Guid taskId) => s_scheduler.Resume(taskId);

        /// <summary>
        /// Cancel a download tasks.
        /// </summary>
        /// <param name="taskId">The ID of the task to cancel.</param>
        public void Cancel(Guid taskId) => s_scheduler.Cancel(taskId);

        public void Dispose()
        {
            s_scheduler.CancelAll();
        }
    }

}
