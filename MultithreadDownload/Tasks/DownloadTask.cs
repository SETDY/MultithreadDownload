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
                if (_state == value) return;

                _state = value;
                StateChange?.Invoke(this, new DownloadDataEventArgs(this));
                // If the state is changed to TaskCompleted or Failed or Cancelled, invoke the TaskCompleted event.
                if (_state is DownloadState.Completed or DownloadState.Failed or DownloadState.Cancelled)
                {
                    // Log the completion of the task.
                    DownloadLogger.LogInfo($"The task with id: {ID} have been completed the download process with state: {_state}.");
                    try
                    {
                        this.TaskCompleted?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        // If the progarmme enter here, that means some methods that scribe to the TaskCompleted event have thrown an exception.
                        // Therefore, log the error and throw an exception has to be excuted.
                        // Otherwise, the task will not be funtionally completed but into a dead loop and the user will not know why the task is not userly completed.
                        DownloadLogger.LogError($"An error occurred when the TaskCompleted event was invoked for the task with id: {ID}.", ex);
                        throw new AggregateException(
                            $"An exception occurred when the TaskCompleted event was invoked for the task with id: {ID}.", ex);
                    }
                }
            }
        }

        /// <summary>
        /// The field of the property State.
        /// </summary>
        private DownloadState _state;

        /// <summary>
        /// The logger instance for logging messages related to the download task.
        /// </summary>
        public DownloadScopedLogger Logger { get; private set; }

        /// <summary>
        /// The thread manager of the task which contain all the threads.
        /// </summary>
        public IDownloadThreadManager ThreadManager { get; private set; }

        /// <summary>
        /// The download speed tracker for monitoring the download speed.
        /// </summary>
        public IDownloadSpeedTracker SpeedTracker { get; private set; }

        /// <summary>
        /// When the state of download is changed, this event will be invoked
        /// </summary>
        public event EventHandler<DownloadDataEventArgs> StateChange;

        /// <summary>
        /// When the download is completed or failed or cancelled, this event will be invoked.
        /// </summary>
        /// <remarks>
        /// It can be seen as the higher implement of StateChange, suitabling some paticulat situation.
        /// </remarks>
        public event Action TaskCompleted;

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
            this.Logger = DownloadLogger.For(Option<string>.Some(this.ID.ToString()), Option<byte>.None());

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
            this.Logger = DownloadLogger.For(Option<string>.Some(this.ID.ToString()), Option<byte>.None());
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
            this.Logger.LogInfo($"The task with id: {ID} is starting to execute the download task.");
            ThreadManager.CreateThreads(downloadService.DownloadFile, DownloadLogger.For(Option<string>.Some(this.ID.ToString()), Option<byte>.None()));
            SetThreadCompletedEventHandler(workProvider, downloadService);
            // Start the download task.
            this.State = DownloadState.Downloading;
            // Log the start of the download
            this.Logger.LogInfo($"The task with id: {ID} have started the download process.");
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

                    // Below code is used to handle that the thread is failed or cancelled.
                    // If the task is cancelled, return to prevent the task from being finalized.
                    if (State is DownloadState.Failed or DownloadState.Cancelled)
                        return;

                    // If the thread is failed, we just set the state to Failed and cancel the task.
                    if (t.State is DownloadState.Failed)
                    {
                        State = DownloadState.Failed;
                        return;
                    }

                    // If all the threads have not completed, return to wait for the next thread to complete.
                    if (ThreadManager.CompletedThreadsCount != ThreadManager.MaxParallelThreads)
                        return;

                    // Below code will be executed when all threads is completed
                    // If the task is already completed, throw an exception.
                    // This is to prevent the task from being finalized multiple times.
                    // Theoretically, this should never happen, but it is a good practice to check it.
                    if (State is DownloadState.Completed)
                        throw new InvalidOperationException($"The task with id: {ID} is already completed but the thread is completed again. ");

                    // Change the set to AfterProcessing to indicate that the task is combining the downloaded parts etc.
                    State = DownloadState.AfterProcessing;
                    // Since the download task has campleted its download, stop the speed monitor.
                    SpeedTracker.Dispose();
                    workProvider.GetTaskFinalStream(DownloadContext)
                        .AndThen(finalStream =>
                            workProvider.Execute_FinalizeWork(finalStream, downloadService, this)
                        ).
                        Match(
                            success =>
                            {
                                // If the finalization is successful, set the state to Completed.
                                // At the same time, the event will be invoked to notify that the download task is completed.
                                // Otherwise, the state of the task sets to Failed.
                                // PS: Never use _state = DownloadState.TaskCompleted; here, because it will not invoke the event.
                                // FIXED: Add a try-catch block to catch the exception and log it to prevent the task from entering a dead loop.
                                try
                                {
                                    // Set the state to Completed.
                                    State = DownloadState.Completed;
                                    // Log the completion of the task.
                                    this.Logger.LogInfo("The task have been finalized successfully.");
                                    // Since the finalization is successful, return true.
                                    return true;
                                }
                                catch (AggregateException ex)
                                {
                                    // Log the exception.
                                    this.Logger.LogInfo($"When change the state of the task to Completed, an exception is thrown which the message is \'{ex.Message}\'.");
                                    // Since the finalization is failed, return false.
                                    return false;
                                }
                            },
                            error =>
                            {
                                // If the finalization fails, set the state to Failed.
                                // At the same time, the event will be invoked to notify that the download task is failed.
                                // PS: Never use _state = DownloadState.TaskCompleted; here, because it will not invoke the event.
                                // FIXED: Add a try-catch block to catch the exception and log it to prevent the task from entering a dead loop.
                                try
                                {
                                    // Set the state to Failed.
                                    State = DownloadState.Failed;
                                    // Log the error message.
                                    this.Logger.LogInfo($"The task with id: {ID} have been failed to finalize due to {error.Message}");
                                }
                                catch (AggregateException ex)
                                {
                                    // Log the exception.
                                    this.Logger.LogInfo($"When change the state of the task to Failed, an exception is thrown which the message is \'{ex.Message}\'.");
                                }
                                // Since the finalization is failed, return false.
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

        public bool Cancel()
        {
            // If the download task is already cancelled or failed, just return.
            // Otherwise, set the state of download task to Cancelled and it will invoke the state change event.
            if (this.State is DownloadState.Cancelled or DownloadState.Failed)
                return false;
            // Set the state to Cancelled.
            State = DownloadState.Cancelled;
            // Cancel all the threads in the thread manager and return the result.
            return ThreadManager.Cancel();
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
        public Result<long, DownloadError> GetCompletedDownloadSize()
        {
            // Check if the thread manager is null.
            if (this.ThreadManager == null)
                return Result<long, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.NullReference, "The thread manager is null."));
            // If not, get the completed size of the file by summing up the completed size of each thread.
            long totalSize = 0;
            foreach (IDownloadThread thread in this.ThreadManager.GetThreads())
            {
                totalSize += thread.CompletedBytesSizeCount;
            }
            return Result<long, DownloadError>.Success(totalSize);
        }

        /// <summary>
        /// Get the number of threads that have been completed download.
        /// </summary>
        /// <returns>The number of thread that have been completed download.</returns>
        public Result<byte, DownloadError> GetCompletedThreadCount()
        {
            // Check if the thread manager is null.
            if (this.ThreadManager == null)
                return Result<byte, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.NullReference, "The thread manager is null."));
            return Result<byte, DownloadError>.Success(this.ThreadManager.CompletedThreadsCount);
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