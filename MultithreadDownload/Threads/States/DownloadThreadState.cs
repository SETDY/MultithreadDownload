using System;
using System.Collections.Generic;
using System.Text;

namespace MultithreadDownload.Threads.States
{
    /// <summary>
    /// The state of a download thread, representing the current status of the download process for a specific segment of the file.
    /// </summary>
    public enum DownloadThreadState
    {
        /// <summary>
        /// The download thread is in the initial state, waiting to be started. 
        /// It has not yet begun the download process.
        /// </summary>
        Pending,
        /// <summary>
        /// The download thread is in the process of starting, transitioning from the Pending state to the Running state. 
        /// It is preparing to begin the download process.
        /// </summary>
        Starting,
        /// <summary>
        /// The download thread is actively downloading the assigned segment of the file. 
        /// It is in the Running state, indicating that the download process is ongoing.
        /// </summary>
        Running,
        /// <summary>
        /// The download thread is in the process of pausing, transitioning from the Running state to the Paused state. 
        /// It is preparing to halt the download process temporarily.
        /// </summary>
        Pausing,
        /// <summary>
        /// The download thread has been paused, indicating that the download process has been temporarily halted.
        /// </summary>
        Paused,
        /// <summary>
        /// The download thread is in the process of resuming, transitioning from the Paused state to the Running state.
        /// It is preparing to continue the download process.
        /// </summary>
        Resuming,
        /// <summary>
        /// The download thread has completed the download process successfully, 
        /// indicating that the assigned segment of the file has been fully downloaded.
        /// </summary>
        Completed,
        /// <summary>
        /// The download thread is in the process of canceling, transitioning from the Running state to the Canceled state.
        /// It is preparing to stop the download process.
        /// </summary>
        Canceling,
        /// <summary>
        /// The download thread has been canceled, indicating that the download process has been stopped and cannot be resumed.
        /// </summary>
        Canceled,
        /// <summary>
        /// The download thread has failed, indicating that an error occurred during the download process, preventing it from completing successfully.
        /// </summary>
        Failed,
        /// <summary>
        /// The download thread has been aborted, indicating that the download process was terminated unexpectedly, 
        /// due to an external interruption or a critical error.
        /// </summary>
        Aborted
    }
}
