using MultithreadDownload.Downloads;
using System.Collections.Generic;

namespace MultithreadDownload.Core
{
    public interface IDownloadTaskScheduler
    {
        public Queue<DownloadTask> TaskQueue { get; set; }

        void Enqueue(DownloadTask task);

        void Start();

        void Stop();
    }
}