using MultithreadDownload.Help;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Downloads
{
    public class DownloadThread
    {
        public int ID { get; set; }

        public string Path { get; set; }

        /// <summary>
        /// 每一个线程应当下载的大小
        /// </summary>
        public long EachThreadShouldDownloadSize { get; internal set; }

        /// <summary>
        /// 线程应当下载的开始位置
        /// </summary>
        public long DownloadPosition { get; internal set; }

        /// <summary>
        /// 下载线程是否存活
        /// </summary>
        public bool IsAlive { get; set; } = true;

        public Thread Thread { get; set; }

        public long CompletedSizeCount { get; internal set; }

        /// <summary>
        /// 完成率
        /// </summary>
        public float CompletionRate { get; internal set; }

        /// <summary>
        /// 刷新文件路径
        /// </summary>
        internal void RefreshPath()
        {
            if (File.Exists(this.Path))
            {
                this.Path = PathHelp.AutomaticFileName(this.Path);
            }
        }

    }
}
