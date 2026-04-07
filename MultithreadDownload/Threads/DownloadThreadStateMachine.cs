using System;
using System.Collections.Generic;
using System.Text;

namespace MultithreadDownload.Threads
{
    /// <summary>
    /// The state machine for a download thread, which manages the state transitions of a download thread based on the operations performed on it, <br/> 
    /// such as starting, pausing, completing, canceling, or failing the download process.
    /// </summary>
    public class DownloadThreadStateMachine
    {
        /// <summary>
        /// The current state of the download thread, representing the current status of the download process for a specific segment of the file.
        /// </summary>
        public DownloadThreadState Current { get; private set; } = DownloadThreadState.Pending;

        /// <summary>
        /// Starts the download thread, transitioning it from the Pending or Paused state to the Running state.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// If the thread is already running or failed or canceled, an exception will be thrown to prevent invalid state transitions.
        /// </exception>
        public void Start()
        {
            this.EnsureState(DownloadThreadState.Pending, DownloadThreadState.Paused);
            this.Current = DownloadThreadState.Running;
        }

        /// <summary>
        /// Pauses the download thread, transitioning it from the Running state to the Paused state.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// If the thread is not currently running, an exception will be thrown to prevent invalid state transitions.
        /// </exception>
        public void Pause()
        {
            this.EnsureState(DownloadThreadState.Running);
            this.Current = DownloadThreadState.Paused;
        }

        /// <summary>
        /// Completes the download thread, transitioning it from the Running state to the Completed state.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// If the thread is not currently running, an exception will be thrown to prevent invalid state transitions.
        /// </exception>
        public void Complete()
        {
            this.EnsureState(DownloadThreadState.Running);
            this.Current = DownloadThreadState.Completed;
        }

        /// <summary>
        /// Cancel the download thread, transitioning it from the Pending, Running, or Paused state to the Canceled state.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// If the thread is already completed, failed, or canceled, an exception will be thrown to prevent invalid state transitions.
        /// </exception>
        public void Cancel()
        {
            this.EnsureState(DownloadThreadState.Pending, DownloadThreadState.Running, DownloadThreadState.Paused, DownloadThreadState.Completed);
            this.Current = DownloadThreadState.Canceled;
        }

        /// <summary>
        /// Fails the download thread, transitioning it from the Running state to the Failed state.
        /// </summary>
        public void Fail()
        {
            this.Current = DownloadThreadState.Failed;
        }

        /// <summary>
        /// Ensures that the current state of the download thread is one of the allowed states for a specific operation, 
        /// throwing an exception if the transition is invalid.
        /// </summary>
        /// <param name="allowedState">
        /// The allowed states for the operation being performed, 
        /// which are used to validate the current state of the download thread before allowing a state transition.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// If the current state of the download thread is not one of the allowed states for the operation being performed,
        /// </exception>
        private void EnsureState(params DownloadThreadState[] allowedState)
        {
            if (!allowedState.Contains(Current))
            {
                throw new InvalidOperationException($"Invalid transition from {Current}.");
            }
        }
    }
}
