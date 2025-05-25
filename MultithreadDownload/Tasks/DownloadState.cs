namespace MultithreadDownload.Downloads
{
    public enum DownloadState
    {
        /// <summary>
        /// The download is waiting to start.
        /// </summary>
        Waiting = 0,
        /// <summary>
        /// The download is currently in progress.
        /// </summary>
        Downloading = 1,
        /// <summary>
        /// The download is during after processing the downloaded parts into a single file.
        /// </summary>
        AfterProcessing = 2,
        /// <summary>
        /// The download is paused and can be resumed later.
        /// </summary>
        Paused = 3,
        /// <summary>
        /// The download has been completed successfully.
        /// </summary>
        Completed = 4,
        /// <summary>
        /// The download has been cancelled by the user but not due to an error.
        /// </summary>
        Cancelled = 5,
        /// <summary>
        /// The download has failed due to an error or an exception.
        /// </summary>
        Failed = 6
    }
}