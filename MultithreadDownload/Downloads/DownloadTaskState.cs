using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Downloads
{
    public enum DownloadTaskState
    {
        /// <summary>
        /// 下载中
        /// </summary>
        Downloading = 0,
        /// <summary>
        /// 等待中
        /// </summary>
        Waiting = 1,
        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 2,
        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = 3
    }
}
