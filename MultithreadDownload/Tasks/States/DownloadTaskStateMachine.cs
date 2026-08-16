using MultithreadDownload.Threads.Events;
using MultithreadDownload.Threads.States;
using System;
using System.Collections.Generic;
using System.Text;

namespace MultithreadDownload.Tasks.States
{
    /// <summary>
    /// The state machine for a download task, which manages the state transitions of a download task based on the operations performed on it.
    /// </summary>
    public class DownloadTaskStateMachine
    {
        /// <summary>
        /// An object used for synchronizing access to the state machine, ensuring that state transitions are task-safe and preventing race conditions
        /// </summary>
        private readonly object _syncRoot = new object();

        /// <summary>
        /// The current state of the download task, representing the status of the download process for a specific segment of the file.
        /// </summary>
        private DownloadTaskState _current = DownloadTaskState.Pending;

        /// <summary>
        /// The current state of the download task, representing the status of the download process for a specific segment of the file.
        /// </summary>
        public DownloadTaskState Current
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
        /// An event that is raised whenever the state of the download task changes, allowing subscribers to be notified of state transitions.
        /// </summary>
        public event EventHandler<DownloadTaskState>? StateChanged;
#nullable disable

        /// <summary>
        /// Attempts to transition the state machine to the "Preparing" state from the "Pending" state, 
        /// indicating that the download task is beginning the download process.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Pending" and the transition to "Preparing" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryBeginPreparing()
        {
            return TryTransition(DownloadTaskState.Preparing, DownloadTaskState.Pending);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Running" state from the "Preparing" state,
        /// indicating that the download task is now running.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Preparing" and the transition to "Running" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryCompletePreparing()
        {
            return TryTransition(DownloadTaskState.Running, DownloadTaskState.Preparing);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Pausing" state from the "Running" state,
        /// indicating that the download task is being paused.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Running" and the transition to "Pausing" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryBeginPause()
        {
            return TryTransition(DownloadTaskState.Pausing, DownloadTaskState.Running);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Paused" state from the "Pausing" state,
        /// indicating that the download task has been paused.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Pausing" and the transition to "Paused" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryCompletePause()
        {
            return TryTransition(DownloadTaskState.Paused, DownloadTaskState.Pausing);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Resuming" state from the "Paused" state,
        /// indicating that the download task is being resumed.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Paused" and the transition to "Resuming" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryBeginResume()
        {
            return TryTransition(DownloadTaskState.Resuming, DownloadTaskState.Paused);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Running" state from the "Resuming" state,
        /// indicating that the download task is now running.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Resuming" and the transition to "Running" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryCompleteResume()
        {
            return TryTransition(DownloadTaskState.Running, DownloadTaskState.Resuming);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Canceling" state from any of the specified states (Pending, Preparing, Running, Pausing, Paused, Resuming),
        /// indicating that the download task is being canceled.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was one of the specified states and the transition to "Canceling" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryBeginCancel()
        {
            return TryTransition(DownloadTaskState.Canceling,
                DownloadTaskState.Pending, DownloadTaskState.Preparing, DownloadTaskState.Running,
                DownloadTaskState.Pausing, DownloadTaskState.Paused, DownloadTaskState.Resuming);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Canceled" state from the "Canceling" state,
        /// indicating that the download task has been canceled.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Canceling" and the transition to "Canceled" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryCompleteCancel()
        {
            return TryTransition(DownloadTaskState.Canceled, DownloadTaskState.Canceling);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Finalizing" state from the "Running" state,
        /// indicating that the download task is in the process of finalizing the download process.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Running" and the transition to "Finalizing" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryBeginFinalize()
        {
            return TryTransition(DownloadTaskState.Finalizing, DownloadTaskState.Running);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Completed" state from the "Finalizing" state,
        /// indicating that the download task has completed the download process successfully.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Finalizing" and the transition to "Completed" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryCompleteFinalize()
        {
            return TryTransition(DownloadTaskState.Completed, DownloadTaskState.Finalizing);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Failed" state from the "Running" state,
        /// indicating that the download task has encountered an error and failed to complete the download process.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Running" and the transition to "Failed" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryComfirmFail()
        {
            return TryTransition(DownloadTaskState.Failed, DownloadTaskState.Running);
        }

        /// <summary>
        /// Attempts to transition the state machine to the "Aborted" state from the "Running" state,
        /// indicating that the download task has been aborted due to an unexpected error or interruption.
        /// </summary>
        /// <returns>
        /// Whether the transition was successful. Returns true if the current state was "Running" and the transition to "Aborted" was successful; 
        /// otherwise, returns false.
        /// </returns>
        internal bool TryComfirmAbort()
        {
            return TryTransition(DownloadTaskState.Aborted, DownloadTaskState.Running);
        }


        private bool TryTransition(DownloadTaskState newState, params DownloadTaskState[] expectState)
        {
            // Lock the synchronization object to ensure task-safe access to the state machine.
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
