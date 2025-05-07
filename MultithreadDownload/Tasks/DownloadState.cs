namespace MultithreadDownload.Downloads
{
    public enum DownloadState
    {
        Waiting = 0,
        Downloading = 1,
        Paused = 2,
        Completed = 3,
        Cancelled = 4
    }
}