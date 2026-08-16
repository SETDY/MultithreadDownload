using MultithreadDownload.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Utils.Threading
{
    /// <summary>
    /// A class that allows pausing and resuming asynchronous operations.
    /// </summary>
    internal class PauseController
    {
        /// <summary>
        /// The TaskCompletionSource that is used to signal when the operation is paused.<br/>
        /// If it has a value, the operation is paused; if it does not have a value, the operation is not paused.
        /// </summary>
        private Option<TaskCompletionSource<bool>> PausedSource { get; set; }

        /// <summary>
        /// Pauses the operation
        /// </summary>
        public void Pause()
        {
            // If the PausedSource does not have a value, we set it to a new TaskCompletionSource.
            if (!this.PausedSource.HasValue)
            {
                // Set the PausedSource to a new TaskCompletionSource, which will be used to signal when the operation is resumed.
                this.PausedSource = Option<TaskCompletionSource<bool>>.Some(new TaskCompletionSource<bool>());
            }

            // The operation cannot be paused if it is already paused, so we throw an exception in that case.
            if (!this.PausedSource.UnwrapOrThrow().Task.Result)
            {
                throw new InvalidOperationException("The operation is already paused.");
            }
        }

        /// <summary>
        /// Resumes the operation
        /// </summary>
        public void Resume()
        {
            // The operation cannot be resumed if it is already running, so we throw an exception in that case.
            if (this.PausedSource.UnwrapOrThrow().Task.Result)
            {
                throw new InvalidOperationException("The operation is already running.");
            }

            this.PausedSource.UnwrapOrThrow().TrySetResult(true);
        }

        /// <summary>
        /// Waits until the operation is resumed if it is currently paused. If it is not paused, it returns immediately.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token to cancel the waiting operation. If the operation is not paused, this token is not used.
        /// </param>
        /// <returns>
        /// The task that represents the asynchronous waiting operation. 
        /// </returns>
        public async Task WaitIfPausedAsync(CancellationToken cancellationToken)
        {
            if (!this.PausedSource.HasValue)
            {
                return;
            }

            await this.PausedSource.UnwrapOrThrow().Task.WaitAsync(cancellationToken);
        }
    }
}
