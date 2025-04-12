using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Core
{
    public interface IDownloadThread
    {
        /// <summary>  
        /// The ID of the download thread. This is used to identify the thread in the download task.  
        /// </summary>  
        public int ID { get; set; }

        /// <summary>  
        /// The download context that contains information about the download operation.  
        /// </summary>  
        public IDownloadContext DownloadContext { get; set; }

        /// <summary>  
        /// The status of the download thread.  
        /// </summary>  
        public bool IsAlive
        {
            get
            {
                if (WorkerThread != null)
                {
                    return WorkerThread.IsAlive;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>  
        /// The thread that will execute the download operation.  
        /// </summary>  
        public Thread WorkerThread { get; set; }

        /// <summary>  
        /// The size of the file that has been downloaded by this thread.  
        /// </summary>  
        public long CompletedBytesSizeCount { get; internal set; }

        /// <summary>
        /// The download progress of the file that has been downloaded by this thread.
        /// </summary>
        public sbyte Progress { get; set; }

        void Start();
        void Pause();
        void Resume();
        void Stop();
    }
}
