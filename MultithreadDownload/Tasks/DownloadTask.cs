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
                // If the state is changed to Completed, log it and invoke the Completed event
                if (this._state == DownloadState.Completed)
                {
                    // Log the completion of the task.
                    DownloadLogger.LogInfo($"The task with id: {ID} have been completed.");
                    try
                    {
                        this.Completed?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        // If the progarmme enter here, that means some methods that scribe to the Completed event have thrown an exception.
                        // Therefore, log the error and throw an exception has to be excuted.
                        // Otherwise, the task will not be funtionally completed but into a dead loop and the user will not know why the task is not userly completed.
                        DownloadLogger.LogError($"An error occurred when the Completed event was invoked for the task with id: {ID}.", ex);
                        throw new InvalidOperationException(
                            $"An error occurred when the Completed event was invoked for the task with id: {ID}.", ex);
                    }
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
        /// Execute this task that is in the queue newly or manually started.
        /// </summary>
        /// <param name="task">The task to start.</param>
        /// <param name="downloadService">The download service to use.</param>
        /// <exception cref="NullReferenceException">The task is null.</exception>
        public Result<bool, DownloadError> ExecuteDownloadTask(IDownloadTaskWorkProvider workProvider, IDownloadService downloadService)
        {
            // Check parameters and properties.
            // If the task already has a state that is not Waiting, return an error message.
            if (State != DownloadState.Waiting)
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.TaskAlreadyStarted, $"The task with id: {ID} is already started or completed. Current state: {State}."));
            // If the download service is null, return an error message.
            if (workProvider == null)
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.NullReference, $"The work provider is null."));

            // Create new doanload threads with the given maximum number of threads.
            // Execute the main work of the task => Start all download threads to download the file
            // Hook the event such that a execution of finalize work can be invoke (e.g. Combine the file segments)
            // when the task is completed.
            // Log the creation of the threads and start the download task.
            DownloadLogger.LogInfo($"The task with id: {ID} is starting to create threads.");
            ThreadManager.CreateThreads(downloadService.DownloadFile);
            SetThreadCompletedEventHandler(workProvider, downloadService);
            // Log the creation of the threads.
            DownloadLogger.LogInfo($"The threads of the task with id: {ID} have been created.");
            // Start the download task.
            this.State = DownloadState.Downloading;
            DownloadLogger.LogInfo($"Downloading {this.ID}");
            return workProvider.Execute_MainWork(downloadService, this);
        }

        private void SetThreadCompletedEventHandler(IDownloadTaskWorkProvider workProvider, IDownloadService downloadService)
        {
            // Log to Set the event handler for when a thread is completed.
            DownloadLogger.LogInfo($"Setting the event handler for when a thread is completed for the task with id: {ID}.");
            this.ThreadManager.ThreadCompleted += (t) =>
            {
                try
                {
                    Debug.WriteLine($"Enter ThreadCompleted process when CompletedThreadsCount is {this.ThreadManager.CompletedThreadsCount} and MaxParallelThreads is {this.ThreadManager.MaxParallelThreads}");
                    // If all the threads have not completed, return to wait for the next thread to complete.
                    if (this.ThreadManager.CompletedThreadsCount !=
                            this.ThreadManager.MaxParallelThreads) { return; }

                    // If the task is already completed or failed, throw an exception.
                    // This is to prevent the task from being finalized multiple times.
                    // Theoretically, this should never happen, but it is a good practice to check it.
                    if (this.State is DownloadState.Completed or DownloadState.Failed)
                    {
                        throw new InvalidOperationException(
                        $"The task with id: {ID} is already completed or failed. Current state: {this.State}");
                    }

                    // Below code will be executed when all threads is completed
                    // Change the set to AfterProcessing to indicate that the task is combining the downloaded parts etc.
                    this.State = DownloadState.AfterProcessing;
                    // Since the download task has campleted its download, stop the speed monitor.
                    SpeedTracker.Dispose();
                    workProvider.GetTaskFinalStream(this.DownloadContext)
                        .AndThen(finalStream =>
                            workProvider.Execute_FinalizeWork(finalStream, downloadService, this)
                        ).
                        Match(
                            success =>
                            {
                                // If the finalization is successful, set the state to Completed.
                                // At the same time, the event will be invoked to notify that the download task is completed.
                                // PS: Never use _state = DownloadState.Completed; here, because it will not invoke the event.
                                // TODO: There is a bug here, the task will not be completed if the event throws an exception.
                                this.State = DownloadState.Completed;
                                DownloadLogger.LogInfo($"The task with id: {ID} have been finalized successfully.");
                                return true;
                            },
                            error =>
                            {
                                // If the finalization fails, set the state to Failed.
                                this.State = DownloadState.Failed;
                                DownloadLogger.LogError($"The task with id: {ID} have been failed to finalize. Error: {error}");
                                return false;
                            }
                        );
                }
                catch(InvalidOperationException iex)
                {
                    DownloadLogger.LogError($"An error occurred when the thread completed for the task with id: {ID}.\n" +
                        $"This may be happen when a method subscribe the task completed event but it throws a exception\n" +
                        $"The message is {iex.Message}", iex);
                    // This exception has to be thrown to prevent the programme from entering a dead loop.
                    throw;
                }
                catch (Exception ex)
                {
                    DownloadLogger.LogError($"Unexpected error occurred when a thread are completed.\nThe message is {ex.Message}", ex);
                    throw;
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
        public static DownloadTask Create(Guid taskID, IDownloadContext downloadContext)
        {
            // If the download context is valid, create a new download task with the given parameters.
            // Otherwise, throw an exception.
            if (downloadContext.IsPropertiesVaild().IsSuccess)
            {
                DownloadLogger.LogInfo($"The task with id: {taskID} is created.");
                return new DownloadTask(taskID, downloadContext.ThreadCount, new DownloadThreadManagerFactory(), downloadContext);
            }
            else
            {
                throw new DownloadContextInvaildException(downloadContext);
            }
        }
    }
}