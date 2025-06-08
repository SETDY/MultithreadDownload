using MultithreadDownload.Core.Errors;
using MultithreadDownload.Downloads;
using MultithreadDownload.Events;
using MultithreadDownload.Exceptions;
using MultithreadDownload.Logging;
using MultithreadDownload.Protocols;
using MultithreadDownload.Schedulers;
using MultithreadDownload.Threading;
using MultithreadDownload.Threads;
using MultithreadDownload.Primitives;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MultithreadDownload.Tasks
{
    /// <summary>
    /// Represents a download task that contains information about the download operation.
    /// </summary>
    public class DownloadTask : IDisposable
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
        public DownloadState State
        {
            get
            {
                return this._state;
            }
            private set
            {
                // Check if the new state is not the same as the current state.
                // Otherwise, return.
                // If not, set the state of the download task and invoke the state change event.
                if (this._state == value) { return; }

                this._state = value;
                this.StateChange?.Invoke(this, new DownloadDataEventArgs(this));
                if (this._state == DownloadState.Completed)
                {
                    this.Completed?.Invoke();
                }
            }
        }

        /// <summary>
        /// The field of the property State.
        /// </summary>
        private DownloadState _state;

        /// <summary>
        /// The thread manager of the task which contain all the threads.
        /// </summary>
        public IDownloadThreadManager ThreadManager { get; private set; }

        /// <summary>
        /// The download speed tracker for monitoring the download speed.
        /// </summary>
        public IDownloadSpeedTracker SpeedTracker { get; private set; }

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

        #endregion Properties

        #region Constructors
        /// <summary>
        /// Initialize a new instance of the <see cref="DownloadTask"/> class with the specified parameters.
        /// </summary>
        /// <param name="taskID">The unique identifier for the download task.</param>
        /// <param name="maxThreads">The maximum number of threads to use for the download task.</param>
        /// <param name="factory">The factory to create the download thread manager.</param>
        /// <param name="downloadContext">The context containing information about the download operation.</param>
        /// <remarks>
        /// This constructor initializes the download task with the specified parameters, designed to be used when the user does not provide a custom speed tracker.
        /// </remarks>
        private DownloadTask(Guid taskID, byte maxThreads, IDownloadThreadManagerFactory factory, IDownloadContext downloadContext)
        {
            // Initialize the download task with the given download delegate, download context ,and factory.
            // Initialize the speed monitor with the method to get the downloaded size.
            this.ID = taskID;
            this._state = DownloadState.Waiting;
            this.DownloadContext = downloadContext;
            this.ThreadManager = factory.Create(new DownloadThreadFactory(),
                this.DownloadContext, maxThreads);
            this.SpeedTracker = new DownloadSpeedTracker();
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="DownloadTask"/> class with the general parameters.
        /// </summary>
        /// <param name="taskID">The unique identifier for the download task.</param>
        /// <param name="maxThreads">The maximum number of threads to use for the download task.</param>
        /// <param name="factory">The factory to create the download thread manager.</param>
        /// <param name="downloadContext">The context containing information about the download operation.</param>
        /// <remarks>
        /// This constructor initializes the download task with the specified parameters, designed to be used when the user provides a custom speed tracker.
        /// </remarks>
        private DownloadTask(Guid taskID, byte maxThreads, IDownloadThreadManagerFactory factory, IDownloadSpeedTracker speedTracker, IDownloadContext downloadContext)
        {
            // Initialize the download task with the given download delegate, download context ,and factory.
            // Initialize the speed monitor with the method to get the downloaded size.
            this.ID = taskID;
            this._state = DownloadState.Waiting;
            this.DownloadContext = downloadContext;
            this.ThreadManager = factory.Create(new DownloadThreadFactory(),
                this.DownloadContext, maxThreads);
            this.SpeedTracker = speedTracker;
        }
        #endregion Constructors

        /// <summary>
        /// Execute a task that is in the queue newly.
        /// </summary>
        /// <param name="task">The task to start.</param>
        /// <param name="downloadService">The download service to use.</param>
        /// <exception cref="NullReferenceException">The task is null.</exception>
        public Result<bool, DownloadErrorCode> ExecuteDownloadTask(IDownloadTaskWorkProvider workProvider, IDownloadService downloadService)
        {
            // Check parameters and properties.
            // If the task already has a state that is not Waiting, return an error message.
            if (State != DownloadState.Waiting)
            {
                DownloadLogger.LogError($"The task with id: {ID} is already started or completed. Current state: {State}.");
                return Result<bool, DownloadErrorCode>.Failure(DownloadErrorCode.TaskAlreadyStarted);
            }
            // If the download service is null, return an error message.
            if (workProvider == null)
            {
                DownloadLogger.LogError($"The work provider is null.");
                return Result<bool, DownloadErrorCode>.Failure(DownloadErrorCode.NullReference);
            }

            // Create new doanload threads with the given maximum number of threads.
            // Execute the main work of the task => Start all download threads to download the file
            // Hook the event such that a execution of finalize work can be invoke (e.g. Combine the file segments)
            // when the task is completed.
            ThreadManager.CreateThreads(downloadService.DownloadFile);
            // Log the creation of the threads.
            //DownloadLogger.LogInfo($"The threads of the task with id: {ID} have been created.");
            // Start the download task.
            this.State = DownloadState.Downloading;
            workProvider.Execute_MainWork(downloadService, this);
            // Log the execution of the main work.
            //DownloadLogger.LogInfo($"The main work of the task with id: {ID} have been executed.");
            this.ThreadManager.ThreadCompleted += (t) =>
            {
                try
                {
                    // If all the threads have not completed, return to wait for the next thread to complete.
                    if (this.ThreadManager.CompletedThreadsCount !=
                            this.ThreadManager.MaxParallelThreads) { return; }

                    // Below code will be executed when all threads is completed
                    // Change the set to AfterProcessing to indicate that the task is combining the downloaded parts etc.
                    this.State = DownloadState.AfterProcessing;
                    // Since the download task has campleted its download, stop the speed monitor.
                    SpeedTracker.Dispose();
                    Result<Stream> finalStream = workProvider.GetTaskFinalStream(this.DownloadContext);
                    if (!finalStream.IsSuccess) { throw new Exception("GetTaskFinalStream failed"); }
                    Result<bool> result = workProvider.Execute_FinalizeWork(finalStream.Value, downloadService, this);
                    if (!result.IsSuccess)
                        this.State = DownloadState.Failed;
                    // Invoke the event to notify that the download task is completed.
                    // PS: Never use _state = DownloadState.Completed; here, because it will not invoke the event.
                    // TODO: There is a bug here, the task will not be completed if the event throws an exception.
                    this.State = DownloadState.Completed;

                    // Log the completion of the task.
                    DownloadLogger.LogInfo($"The task with id: {ID} have been completed.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unexpected error occurred when a thread are completed.\nThe message is {ex.Message}");
                }
            };
        }

        // TODO: Complete the pause and resume function
        internal void Pause()
        {
            throw new NotImplementedException();
        }

        internal void Resume()
        {
            throw new NotImplementedException();
        }

        public void Cancel()
        {
            // If the download task is already cancelled, return an error message.
            // Otherwise, cancel the download task and it will invoke the state change event.
            if (this.State == DownloadState.Cancelled) { throw new InvalidOperationException("The download task is already cancelled."); }
            this.State = DownloadState.Cancelled;
            this.ThreadManager.Cancel();
        }

        public void Dispose()
        {
            // Dispose the download task and release the resources.
            this.Cancel();
            this.ThreadManager.Dispose();
            this.SpeedTracker.Dispose();
        }

        /// <summary>
        /// Get the completed size of the file that is being downloaded by this task.
        /// </summary>
        /// <returns></returns>
        public Result<long> GetCompletedDownloadSize()
        {
            if (this.ThreadManager == null) { return Result<long>.Failure("The thread manager is null."); }
            long size = 0;
            foreach (IDownloadThread thread in this.ThreadManager.GetThreads())
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
            if (this.ThreadManager != null)
            {
                return Result<byte>.Success((byte)this.ThreadManager.CompletedThreadsCount);
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
        public static DownloadTask Create(Guid taskID, byte maxThreads, IDownloadContext downloadContext)
        {
            // If the download context is valid, create a new download task with the given parameters.
            // Otherwise, throw an exception.
            if (downloadContext.IsPropertiesVaild().IsSuccess)
            {
                DownloadLogger.LogInfo($"The task with id: {taskID} is created.");
                return new DownloadTask(taskID, maxThreads, new DownloadThreadManagerFactory(), downloadContext);
            }
            else
            {
                throw new DownloadContextInvaildException(downloadContext);
            }
        }
    }
}