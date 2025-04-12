using MultithreadDownload.Core;
using MultithreadDownload.Downloads;
using MultithreadDownload.Exceptions;
using MultithreadDownload.Help;
using MultithreadDownload.Threading;
using MultithreadDownload.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MultithreadDownload.Tasks
{
    /// <summary>
    /// Represents a download task that contains information about the download operation.
    /// </summary>
    public class DownloadTask
    {
        #region Properties
        /// <summary>
        /// Task ID for identifying the download task.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The download context that contains information about the download operation.
        /// </summary>
        public IDownloadContext DownloadContext { get; private set; }

        /// <summary>
        /// The DownloadThread list that contains all the DownloadThread(Not thread!) for this download task.
        /// </summary>
        public List<DownloadThread> DownloadThreads { get; private set; }

        /// <summary>
        /// The state of the download task.
        /// </summary>
        public DownloadTaskState State { get; private set; }

        /// <summary>
        /// The downloa speed monitor for monitoring the download speed.
        /// </summary>
        private DownloadSpeedMonitor s_speedMonitor = new DownloadSpeedMonitor();
        #endregion


        /// <summary>
        /// Get the number of threads that have been completed download.
        /// </summary>
        /// <returns>The number of thread that have been completed download.</returns>
        public Result<byte> GetCompletedThreadCount()
        {
            if (this.DownloadThreads != null)
            {
                return Result<byte>.Success((byte)this.DownloadThreads.
                    Count(t => t.IsAlive == false));
            }
            else
            {
                return Result<byte>.Failure("The download thread list is null.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="path"></param>
        public static DownloadTask Create(int ID, string url, string path, MultiDownload target)
        {
            if (NetWorkHelp.CanConnectionUrlAsync(url).GetAwaiter().GetResult())//链接是否可以连接
            {//是
                if (path[path.Length - 1] != '\\')//为了修复Path.Combine方法的缺陷，防止路径合并错误
                {
                    path = path + '\\';
                }
                //获取下载路径
                string downloadPath = System.IO.Path.Combine(path + System.IO.Path.GetFileName(HttpUtility.UrlDecode(url)));//合并路径
                DownloadTask downloadTask = new DownloadTask(target);//新建下载任务
                downloadTask.Path = downloadPath;//将路径赋值
                downloadTask.Url = url;//将链接赋值
                downloadTask.State = DownloadTaskState.Waiting;
                return downloadTask;
            }
            else
            {//否
                throw new UrlCanNotConnectionException(url);//抛出连接无法连接错误
            }
        }

    }
}
