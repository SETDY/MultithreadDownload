using MultithreadDownload.Downloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
