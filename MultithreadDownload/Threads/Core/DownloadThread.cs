using MultithreadDownload.Core.Errors;
using MultithreadDownload.Core.Primitives;
using MultithreadDownload.Exceptions;
using MultithreadDownload.Logging;
using MultithreadDownload.Primitives;
using MultithreadDownload.Protocols.Abstractions;
using MultithreadDownload.Threads.Abstractions;
using MultithreadDownload.Threads.Events;
using MultithreadDownload.Threads.States;
using MultithreadDownload.Utils.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Threads.Core
{
    /// <summary>
    /// The download process for a specific segment of the file, <br/>
    /// which is responsible for downloading a specific segment of the file from the network and writing it to the disk.
    /// </summary>
    /// <typeparam name="TTarget">
    /// The download target type, which represents the information of the file to be downloaded, such as the URL, file name, file size, etc. <br/>
    /// </typeparam>
    public class DownloadThread<TTarget> : IDownloadThread<TTarget>, IDisposable where TTarget : IDownloadTarget
    {
        public DownloadThreadContext Context { get; }

        private TTarget Target { get; }

        private IDownloadService<TTarget> Service { get; }

        private CancellationTokenSource CancellationToken { get; } = new CancellationTokenSource();

        private DownloadScopedLogger Logger { get; }

        private PauseController PauseController { get; }

#nullable enable
        /// <summary>
        /// The event that is triggered when the download thread reports progress, 
        /// which includes the current context of the download thread and delta bytes.
        /// </summary>
        public event Action<DownloadThreadDataEventArgs, long>? ProgressChanged;

        /// <summary>
        /// The event that is triggered when the download thread changes its state.
        /// </summary>
        public event EventHandler<DownloadThreadDataEventArgs>? StateChange;

        /// <summary>
        /// The event that is triggered when the download thread fails, 
        /// which includes the current context of the download thread and the error information of the failure.
        /// </summary>
        public event Action<DownloadThreadDataEventArgs, DownloadError>? OnFailed;

#nullable disable

        public DownloadThread(DownloadThreadContext context, TTarget target, IDownloadService<TTarget> service, DownloadScopedLogger logger)
        {
            // Initionlize the properties of the download thread
            this.Target = target;
            this.Context = context;
            this.Service = service;
            this.Logger = logger;
            this.PauseController = new PauseController();

            // Subscribe to the StateChange event of the state machine to
            // invoke the StateChange event of the download thread when the state of the state machine is changed.
            this.Context.StateMachine.StateChanged += (object sender, DownloadThreadState e) =>
            {
                this.StateChange?.Invoke(this, new DownloadThreadDataEventArgs(this.Context));
            };
        }

        /// <summary>
        /// Starts the download thread, which will continuously download chunks of data until the download is complete or failed.
        /// </summary>
        /// <param name="inputStream">
        /// The input stream from which the download thread will read data, which is usually the network stream of the download connection.
        /// </param>
        /// <param name="outputStream">
        /// The output stream to which the download thread will write data, which is usually the file stream of the target file on disk.
        /// </param>
        /// <returns>
        /// The task that represents the asynchronous download operation.
        /// </returns>
        public async Task StartAsync(Stream outputStream)
        {
            this.Context.StateMachine.TryBeginStart();
            this.Context.StateMachine.TryCompleteStart();
            await this.DownloadAsync(outputStream);
        }

        private async Task DownloadAsync(Stream outputStream)
        {
            try
            {
                // Continuously download chunks of data until the download is complete or failed or requesting to be cancelled,
                // which is determined by the current position of the download thread and the state of the state machine.
                while (this.Context.CurrentPostion < Context.EndPositon)
                {
                    // Check if the download is aborted by invoking the ThrowIfCancellationRequested method of the CancellationToken,
                    // which will throw an OperationCanceledException if the cancellation is requested.
                    this.CancellationToken.Token.ThrowIfCancellationRequested();

                    // Pause the download thread if the state of the state machine is pausing
                    await this.PauseController.WaitIfPausedAsync(this.CancellationToken.Token);

                    // Oterwise, get the input stream for the current chunk of data to be downloaded to
                    // download a chunk of data from the input stream to the output stream.
                    // If the download operation is failed, thrown a exception with the error information of the download error.
                    Stream inputStream = (await this.Service.GetChunkStreamAsync(
                        this.Target,
                        this.Context.CurrentPostion,
                        this.Context.CurrentPostion + this.Context.ChunkSize - 1,
                        CancellationToken.Token,
                        this.Logger)).UnwrapOrThrow();
                    // After the chunk of data is downloaded successfully, get the actural byte count of the downloaded chunk.
                    long byteCount = (await this.Service.DownloadChunkAsync(
                        inputStream,
                        outputStream,
                        this.Context.ChunkSize,
                        CancellationToken.Token,
                        this.Logger))
                        .UnwrapOrThrow() ;
                    // Update the current position of the download thread by adding the delta bytes of the downloaded chunk to the current position.
                    this.Context.UpdateCurrentPosition(byteCount);
                    // Report the progress of the download thread by invoking the ProgressChanged event with the current context and delta bytes.
                    this.ProgressChanged?.Invoke(new DownloadThreadDataEventArgs(this.Context), byteCount);
                    // Ensure the state of the download thread is running,
                    // which will throw an exception if the current state of the download thread is not running or pausing.
                    this.Context.StateMachine.EnsureState(DownloadThreadState.Running, DownloadThreadState.Pausing);
                }
                // Set the state of the download thread to completed
                this.Context.StateMachine.Complete();
            }
            catch (OperationCanceledException)
            {
                // If the download is aborted by the cancellation request, log the cancellation of the download thread
                this.Logger.LogInfo($"Download thread is aborted.");
            }
            catch (ResultFailureException<DownloadError> ex)
            {
                // If the download operation fails with a known download error, change the state of the download thread to failed and
                // invoke the OnFailed event with the error information of the download error.
                this.Context.StateMachine.Fail();
                this.OnFailed?.Invoke(new DownloadThreadDataEventArgs(this.Context), ex.Error);
            }
            catch (InvalidOperationException)
            {
                // Log the stopping of the download thread
                this.Logger.LogInfo($"Download thread has beens stopped gracefully.");
            }
            catch (Exception ex)
            {
                // If an unexpected or unknown exception occurs during the download process,
                // change the state of the download thread to failed and
                // invoke the OnFailed event with the error information of the exception and also
                this.Context.StateMachine.Fail();
                this.OnFailed?.Invoke(
                    new DownloadThreadDataEventArgs(this.Context), 
                    DownloadError.Create(DownloadErrorCode.UnexpectedOrUnknownException, ex.Message));
            }
            finally
            {
                // Invoke the OnStateChange event to notify the state change of the download thread.
                this.OnStateChange?.Invoke(new DownloadThreadDataEventArgs(this.Context));
            }
        }

        /// <summary>
        /// Cancels the download thread, which will trigger the OperationCanceledException in the StartAsync method and change the state of the download thread to cancelled.
        /// </summary>
        /// <returns>
        /// The task that represents the asynchronous cancellation operation
        /// </returns>
        public async Task CancelAsync()
        {
            // Log the cancellation of the download thread
            this.Logger.LogInfo($"Cancelling download thread with id: {this.Context.Id}");
            // Change the state of the download thread to cancelled by invoking the Cancel method of the state machine.
            this.Context.StateMachine.Canceling();

        }

        /// <summary>
        /// Aborts the download thread, which will change the state of the download thread to aborted and release all the resources used by the download thread immediately.
        /// </summary>
        /// <returns></returns>
        public async Task AbortAsync()
        {
            // Log the abortion of the download thread
            this.Logger.LogInfo($"Aborting download thread with id: {this.Context.Id}");

            // Cancel the download operation by invoking the Cancel method of the CancellationTokenSource.
            await this.CancellationToken.CancelAsync();
            // Change the state of the download thread to aborted by invoking the Abort method of the state machine.
            this.Context.StateMachine.Abort();
        }

        /// <summary>
        /// Pauses the download thread, which will change the state of the download thread to paused.
        /// </summary>
        /// <returns>
        /// The task that represents the asynchronous pause operation
        /// </returns>
        public async Task PauseAsync()
        {
            // Log the pausing of the download thread
            this.Logger.LogInfo($"Pausing download thread with id: {this.Context.Id}");

            // Change the state of the download thread to be pausing by invoking the Pauseing method of the state machine.
            this.Context.StateMachine.Pauseing();
            // Pause the operaion
            this.PauseController.Pause();
            // Invoke the OnStateChange event to notify the state change of the download thread.
            this.OnStateChange?.Invoke(new DownloadThreadDataEventArgs(this.Context));
        }

        /// <summary>
        /// Resumes the download thread, which will change the state of the download thread from paused to downloading and continue the download process.
        /// </summary>
        /// <returns>
        /// The task that represents the asynchronous resuming operation
        /// </returns>
        public async Task ResumeAsync()
        {
            // Change the state of the download thread to be resuming by invoking the Resuming method of the state machine.
            this.Context.StateMachine.Resume();
            // Resume the operaion
            this.PauseController.Resume();
            // Invoke the OnStateChange event to notify the state change of the download thread.
            this.OnStateChange?.Invoke(new DownloadThreadDataEventArgs(this.Context));
        }

        public void Dispose()
        {
            this.CancellationToken.Dispose();
        }
    }
}
