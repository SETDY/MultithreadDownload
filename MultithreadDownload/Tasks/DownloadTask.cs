using MultithreadDownload.Core;
using MultithreadDownload.Downloads;
using MultithreadDownload.Events;
using MultithreadDownload.Exceptions;
using MultithreadDownload.Threading;
using MultithreadDownload.Threads;
using MultithreadDownload.Utils;
using System;
using System.Runtime.CompilerServices;

namespace MultithreadDownload.Tasks
{
    /// <summary>
    /// Represents a download task that contains information about the download operation.
    /// </summary>
    public class DownloadTask
    {
        #region Properties

        /// <summary>
        /// Task ID for identifying the download task.
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// The download context that contains information about the download operation.
        /// </summary>
        public IDownloadContext DownloadContext { get; private set; }

        /// <summary>
        /// The state of the download task.
        /// </summary>
        public DownloadTaskState State
        {
            get
            {
                return this._state;
            }
            private set
            {
                // Set the state of the download task and invoke the state change event.
                this._state = value;
                this.StateChange?.Invoke(this, new DownloadDataEventArgs(this));
                if (this._state == DownloadTaskState.Completed)
                {
                    this.Completed?.Invoke();
                }
            }
        }

        /// <summary>
        /// The field of the property State.
        /// </summary>
        private DownloadTaskState _state;

        /// <summary>
        /// The thread manager of the task which contain all the threads.
        /// </summary>
        public IDownloadThreadManager DownloadThreadManager { get; private set; }

        /// <summary>
        /// When the state of download is completed, this event will be invoked
        /// </summary>
        public event EventHandler<DownloadDataEventArgs> StateChange;

        /// <summary>
        /// When the download is completed, this event will be invoked.
        /// </summary>
        /// <remarks>
        /// It can be seen as the higher implement of StateChange, suitabling some paticulat situation.
        /// </remarks>
        public event Action Completed;

        /// <summary>
        /// The download speed monitor for monitoring the download speed.
        /// </summary>
        public readonly DownloadSpeedMonitor SpeedMonitor = new DownloadSpeedMonitor();

        #endregion Properties

        private DownloadTask(Guid taskID, byte maxThreads ,DownloadWorkDelegate mainDownloadWorkDelegate, IDownloadThreadManagerFactory factory, IDownloadContext downloadContext)
        {
            // Initialize the download task with the given download delegate, download context ,and factory.
            // Initialize the speed monitor with the method to get the downloaded size.
            this.ID = taskID;
            this._state = DownloadTaskState.Waiting;
            this.DownloadContext = downloadContext;
            this.DownloadThreadManager = factory.Create(new DownloadThreadFactory(),
                this.DownloadContext, mainDownloadWorkDelegate, maxThreads);
            this.StartSpeedMonitor();
        }

        /// <summary>
        /// Start the download speed monitor.
        /// </summary>
        /// <exception cref="NullReferenceException">The download thread manager is null.</exception>
        private void StartSpeedMonitor()
        {
            // Start the speed monitor with the method to get the downloaded size.
            this.SpeedMonitor.Start(() =>
            {
                Result<long> result = this.GetCompletedDownloadSize();
                if (result.IsSuccess)
                {
                    return result.Value;
                }
                else
                {
                    throw new NullReferenceException(result.ErrorMessage);
                }
            });
        }

        public Result<bool> Start()
        {
            // If the download task is already running, return an error message.
            // Otherwise, start the download task and it will invoke the state change event.
            if (this.State == DownloadTaskState.Downloading) { return Result<bool>.Failure("The download task is already running."); }

            this.State = DownloadTaskState.Downloading;
            this.DownloadThreadManager.Start();
            return Result<bool>.Success(true);
        }

        public Result<bool> Start(Action onComplete)
        {
            // If the download task is already running, return an error message.
            // Otherwise, start the download task and it will invoke the state change event.
            // After the task is completed, onComplete action will be invoked.
            if (this.State == DownloadTaskState.Downloading) { return Result<bool>.Failure("The download task is already running."); }

            this.State = DownloadTaskState.Downloading;
            this.DownloadThreadManager.Start();
            this.Completed += onComplete;
            return Result<bool>.Success(true);
        }

        //TODO: 完成断点续传功能
        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }

        public void Cancel()
        {
            // If the download task is already cancelled, return an error message.
            // Otherwise, cancel the download task and it will invoke the state change event.
            if (this.State == DownloadTaskState.Cancelled) { throw new InvalidOperationException("The download task is already cancelled."); }
            this.State = DownloadTaskState.Cancelled;
            this.DownloadThreadManager.Cancel();
        }

        /// <summary>
        /// Get the completed size of the file that is being downloaded by this task.
        /// </summary>
        /// <returns></returns>
        public Result<long> GetCompletedDownloadSize()
        {
            if (this.DownloadThreadManager == null) { return Result<long>.Failure("The thread manager is null."); }
            long size = 0;
            foreach (IDownloadThread thread in this.DownloadThreadManager.GetThreads())
            {
                size += thread.CompletedBytesSizeCount;
            }
            return Result<long>.Success(size);
        }

        /// <summary>
        /// Get the number of threads that have been completed download.
        /// </summary>
        /// <returns>The number of thread that have been completed download.</returns>
        public Result<byte> GetCompletedThreadCount()
        {
            if (this.DownloadThreadManager != null)
            {
                return Result<byte>.Success((byte)this.DownloadThreadManager.CompletedThreadsCount);
            }
            else
            {
                return Result<byte>.Failure("The download thread list is null.");
            }
        }

        /// <summary>
        /// Create a new download task with the given parameters.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="path"></param>
        public static DownloadTask Create(Guid ID, DownloadWorkDelegate workDelegate, IDownloadContext downloadContext)
        {
            // If the download context is valid, create a new download task with the given parameters.
            // Otherwise, throw an exception.
            if (downloadContext.IsPropertiesVaild().IsSuccess)
            {
                return new DownloadTask(ID, workDelegate,
                    new DownloadThreadManagerFactory(), downloadContext);
            }
            else
            {
                throw new DownloadContextInvaildException(downloadContext);
            }
        }
    }
}