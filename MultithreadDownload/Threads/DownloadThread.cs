using MultithreadDownload.Core.Errors;
using MultithreadDownload.Protocols.Abstractions;
using MultithreadDownload.Threads.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Threads
{
    public class DownloadThread
    {
        public DownloadThreadContext Context { get; }

        private IDownloadService Service { get; }

        private CancellationTokenSource CancellationToken { get; } = new CancellationTokenSource();

#nullable enable
        /// <summary>
        /// The event that is triggered when the download thread reports progress, 
        /// which includes the current context of the download thread and the total bytes downloaded so far.
        /// </summary>
        public event Action<DownloadThreadDataEventArgs, long>? ProgressChanged;

        /// <summary>
        /// The event that is triggered when the download thread changes its state.
        /// </summary>
        public event Action<DownloadThreadDataEventArgs, DownloadThreadState>? OnStateChange;

        /// <summary>
        /// The event that is triggered when the download thread is completed, 
        /// which includes the current context of the download thread.
        /// </summary>
        public event Action<DownloadThreadDataEventArgs> OnCompleted;

        /// <summary>
        /// The event that is triggered when the download thread is failed, 
        /// which includes the current context of the download thread and the error information of the failure.
        /// </summary>
        public event Action<DownloadThreadDataEventArgs, DownloadError>? OnFailed;
        #nullable disable

        public DownloadThread(DownloadThreadContext context, IDownloadService service)
        {
            this.Context = context;
            this.Service = service;
        }

        /// <summary>
        /// Starts the download thread, which will continuously download chunks of data until the download is complete or failed.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public async Task StartAsync(Stream inputStream, Stream outputStream)
        {
            try
            {
                // Invoke the state machine to start the download process.
                this.Context.StateMachine.Start();

                // Continuously download chunks of data until the download is complete or failed.
                while (this.Context.CurrentPostion < Context.EndPositon)
                {
                    this.CancellationToken.Token.ThrowIfCancellationRequested();

                    Result<> downloadChunkResult = await this.Service.DownloadChunkAsync(
                        inputStream,
                        outputStream,
                        Context,
                        CancellationToken.Token);

                    if (result.IsFailure)
                    {
                        throw new InvalidOperationException($"Failed to download chunk at position {Context.CurrentPostion} with error: {result.Error}");
                    }

                    long bytes = result.Value;

                    Context.CurrentOffset += bytes;

                    ProgressChanged?.Invoke(bytes);
                }

                Context.StateMachine.Complete();
                Completed?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // If the download is cancelled, change the state of the download thread to cancelled and
                // invoke the OnFailed event with the cancellation error information.
                this.Context.StateMachine.Cancel();
                this.OnStateChange?.Invoke(
                    new DownloadThreadDataEventArgs(this.Context),
                    this.Context.StateMachine.Current);
            }
            catch (Exception ex)
            {
                // If an unexpected or unknown exception occurs during the download process,
                // change the state of the download thread to failed and
                // invoke the OnFailed event with the error information of the exception and also
                // invoke the OnStateChange event to notify the state change of the download thread.
                this.Context.StateMachine.Fail();
                this.OnFailed?.Invoke(
                    new DownloadThreadDataEventArgs(this.Context),
                    DownloadError.Create(DownloadErrorCode.UnexpectedOrUnknownException, ex.Message));
                this.OnStateChange?.Invoke(
                    new DownloadThreadDataEventArgs(this.Context),
                    this.Context.StateMachine.Current);
            }
        }
    }
}
