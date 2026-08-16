using MultithreadDownload.Core.Errors;
using MultithreadDownload.Downloads;
using MultithreadDownload.Exceptions;
using MultithreadDownload.Logging;
using MultithreadDownload.Primitives;
using MultithreadDownload.Protocols.Abstractions;
using MultithreadDownload.Threads.Core;
using MultithreadDownload.Threads.Events;
using MultithreadDownload.Utils.Threading;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Threads.Abstractions
{
    public interface IDownloadThread<TTarget> : IDisposable where TTarget : IDownloadTarget
    {
        public DownloadThreadContext Context { get; }

#nullable enable
        /// <summary>
        /// The event that is triggered when the download thread reports progress, 
        /// which includes the current context of the download thread and delta bytes.
        /// </summary>
        public event Action<DownloadThreadDataEventArgs, long>? ProgressChanged;

        /// <summary>
        /// The event that is triggered when the download thread changes its state.
        /// </summary>
        public event Action<DownloadThreadDataEventArgs>? OnStateChange;

        /// <summary>
        /// The event that is triggered when the download thread fails, 
        /// which includes the current context of the download thread and the error information of the failure.
        /// </summary>
        public event Action<DownloadThreadDataEventArgs, DownloadError>? OnFailed;

#nullable disable

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
        public Task StartAsync(Stream outputStream);

        /// <summary>
        /// Cancels the download thread, which will trigger the OperationCanceledException in the StartAsync method and change the state of the download thread to cancelled.
        /// </summary>
        /// <returns>
        /// The task that represents the asynchronous cancellation operation
        /// </returns>
        public Task CancelAsync();

        /// <summary>
        /// Aborts the download thread, which will change the state of the download thread to aborted and release all the resources used by the download thread immediately.
        /// </summary>
        /// <returns></returns>
        public Task AbortAsync();

        /// <summary>
        /// Pauses the download thread, which will change the state of the download thread to paused.
        /// </summary>
        /// <returns>
        /// The task that represents the asynchronous pause operation
        /// </returns>
        public Task PauseAsync();

        /// <summary>
        /// Resumes the download thread, which will change the state of the download thread from paused to downloading and continue the download process.
        /// </summary>
        /// <returns>
        /// The task that represents the asynchronous resuming operation
        /// </returns>
        public Task ResumeAsync();

        public void Dispose();
    }
}