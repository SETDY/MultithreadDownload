using MultithreadDownload.Tasks.States;
using MultithreadDownload.Threads.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace MultithreadDownload.Threads.States
{
    /// <summary>
    /// The state machine for a download thread, which manages the state transitions of a download thread based on the operations performed on it.
    /// </summary>
    public sealed class DownloadTaskStateMachine
    {
        /// <summary>
        /// An object used for synchronizing access to the state machine, ensuring that state transitions are thread-safe and preventing race conditions
        /// </summary>
        private readonly object _syncRoot = new object();

        /// <summary>
        /// The current state of the download thread, representing the status of the download process for a specific segment of the file.
        /// </summary>
        private DownloadThreadState _current = DownloadThreadState.Pending;

        /// <summary>
        /// The current state of the download thread, representing the status of the download process for a specific segment of the file.
        /// </summary>
        public DownloadThreadState Current
        {
            get
            {
                lock (_syncRoot)
                {
                    return _current;
                }
            }
        }

#nullable enable
        /// <summary>
        /// An event that is raised whenever the state of the download thread changes, allowing subscribers to be notified of state transitions.
        /// </summary>
        public event EventHandler<DownloadThreadState>? StateChanged;
#nullable disable

        /// <summary>
        /// Attempts to transition the state machine to the "Starting" state from the "Pending" state, 
        /// indicating that the download thread is beginning the download process.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Pending" and the transition to "Starting" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryBeginStart()
        {
            return TryTransition(DownloadThreadState.Starting, DownloadThreadState.Pending);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Running" state from the "Starting" state,
        /// indicating that the download thread is now running.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Starting" and the transition to "Running" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryCompleteStart()
        {
            return TryTransition(DownloadThreadState.Running, DownloadThreadState.Starting);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Pausing" state from the "Running" state,
        /// indicating that the download thread is being paused.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Running" and the transition to "Pausing" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryBeginPause()
        {
            return TryTransition(DownloadThreadState.Pausing, DownloadThreadState.Running);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Paused" state from the "Pausing" state,
        /// indicating that the download thread has been paused.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Pausing" and the transition to "Paused" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryCompletePause()
        {
            return TryTransition(DownloadThreadState.Paused, DownloadThreadState.Pausing);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Resuming" state from the "Paused" state,
        /// indicating that the download thread is being resumed.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Paused" and the transition to "Resuming" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryBeginResume()
        {
            return TryTransition(DownloadThreadState.Resuming, DownloadThreadState.Paused);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Running" state from the "Resuming" state,
        /// indicating that the download thread is now running.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Resuming" and the transition to "Running" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryCompleteResume()
        {
            return TryTransition(DownloadThreadState.Running, DownloadThreadState.Resuming);
        }
        
        /// <summary>
        /// Attempts to transition the state machine to the "Canceling" state from any of the specified states (Pending, Starting, Running, Pausing, Paused, Resuming),
        /// indicating that the download thread is being canceled.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was one of the specified states and the transition to "Canceling" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryBeginCancel()
        {
            return TryTransition(DownloadThreadState.Canceling, 
                DownloadThreadState.Pending, DownloadThreadState.Starting, DownloadThreadState.Running, 
                DownloadThreadState.Pausing, DownloadThreadState.Paused, DownloadThreadState.Resuming);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Canceled" state from the "Canceling" state,
        /// indicating that the download thread has been canceled.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Canceling" and the transition to "Canceled" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryCompleteCancel()
        {
            return TryTransition(DownloadThreadState.Canceled, DownloadThreadState.Canceling);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Completed" state from the "Running" state,
        /// indicating that the download thread has completed the download process successfully.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Running" and the transition to "Completed" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryCompleteRunning()
        {
            return TryTransition(DownloadThreadState.Completed, DownloadThreadState.Running);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Failed" state from the "Running" state,
        /// indicating that the download thread has encountered an error and failed to complete the download process.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Running" and the transition to "Failed" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryComfirmFail()
        {
            return TryTransition(DownloadThreadState.Failed, DownloadThreadState.Running);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Aborted" state from the "Running" state,
        /// indicating that the download thread has been aborted due to an unexpected error or interruption.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Running" and the transition to "Aborted" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryComfirmAbort()
        {
            return TryTransition(DownloadThreadState.Aborted, DownloadThreadState.Running);
        }

        private bool TryTransition(DownloadThreadState newState, params DownloadThreadState[] expectState)
        {
            // Lock the synchronization object to ensure thread-safe access to the state machine.
            lock (_syncRoot)
            {
                // If the current state is one of the expected states, transition to the new state and return true; otherwise, return false.
                if (Array.Exists(expectState, state => state == _current))
                {
                    _current = newState;
                    return true;
                }
                return false;
            }
        }
    }
}
