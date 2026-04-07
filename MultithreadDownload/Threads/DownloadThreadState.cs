using System;
using System.Collections.Generic;
using System.Text;

namespace MultithreadDownload.Threads
{
    /// <summary>
    /// The state of a download thread, representing the current status of the download process for a specific segment of the file.
    /// </summary>
    public enum DownloadThreadState
    {
        /// <summary>
        /// Waiting to start the download. 
        /// The thread is initialized but has not yet begun downloading.
        /// </summary>
        Pending,
        /// <summary>
        /// The download is currently in progress. 
        /// The thread is actively downloading data from the source.
        /// </summary>
        Running,
        /// <summary>
        /// The download has been paused. 
        /// The thread has temporarily stopped downloading, but it can be resumed later to continue from where it left off.
        /// </summary>
        Paused,
        /// <summary>
        /// The download has been completed successfully.
        /// The thread has finished downloading all the data without any errors, and the file (segement) is ready for use.
        /// </summary>
        Completed,
        /// <summary>
        /// The download has been canceled by the user.
        /// </summary>
        Canceled,
        /// <summary>
        /// The download has failed due to an error.
        /// </summary>
        Failed 
    }
}
