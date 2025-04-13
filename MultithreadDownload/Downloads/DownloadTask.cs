﻿using MultithreadDownload.Events;
using MultithreadDownload.Exceptions;
using MultithreadDownload.Ways;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Timers;
using System.Web;

namespace MultithreadDownload.Downloads
{
    public class DownloadTask
    {
        /// <summary>
        /// 下载任务ID
        /// </summary>
        public int ID { get; internal set; }

        public string Url { get; internal set; }

        public string Path { get; internal set; }

        /// <summary>
        /// 完成下载的线程数量
        /// </summary>
        public byte CompleteDownloadThreadCount { get; internal set; }

        /// <summary>
        /// 任务的状态
        /// </summary>
        public DownloadTaskState State
        {
            get
            {
                return this.stateCache;
            }
            internal set
            {
                if (this.stateCache != value)
                {
                    this.stateCache = value;
                    this.InvokeStateChangeEvent
                        (System.IO.Path.GetFileName(this.Path), this.Url, this.Path,
                        this.stateCache);//触发事件
                }
            }
        }

        private DownloadTaskState stateCache;

        public List<DownloadThread> Threads { get; internal set; }

        public string Tag { get; set; }

        /// <summary>
        /// 下载项目的目标下载类的储存
        /// </summary>
        public MultiDownload Target { get; private set; }

        /// <summary>
        /// 当下载任务的状态改变
        /// </summary>
        public event EventHandler<DownloadDataEventArgs> StateChange;

        /// <summary>
        /// 每一秒执会触发此事件一次 (用以刷新数据)
        /// </summary>
        public event EventHandler<DownloadDataEventArgs> Refresh;

        /// <summary>
        /// 负责这个Task的计时器
        /// </summary>
        private System.Timers.Timer downloadTaskTimer = new System.Timers.Timer() { Interval = 1000 };

        /// <summary>
        /// 上一秒下载总大小
        /// </summary>
        private long lastSecondDownloadSize { get; set; }

        /// <summary>
        /// 这一秒下载总大小
        /// </summary>
        private long theSecondDownloadSize
        {
            get
            {
                long total = 0;
                foreach (DownloadThread downloadThread in this.Threads)
                {
                    total += downloadThread.CompletedSizeCount;
                }
                return total;
            }
        }

        internal object DownloadLocker = new object();

        /// <summary>
        /// 完成率
        /// </summary>
        public float CompletionRate
        {
            get
            {
                float totalCompletionRate = 0;
                if (this.State == DownloadTaskState.Downloading)//防止过快获得完成度而产生NULL异常
                {
                    //遍历所有下载线程
                    foreach (DownloadThread thread in this.Threads)
                    {
                        totalCompletionRate += thread.CompletionRate * (1F / this.Threads.Count);//算出每个线程的占比完成率[最终线程完成率 = 线程完成率 * (1 / 线程数)]
                    }
                }
                else if (this.State == DownloadTaskState.Completed)
                {
                    totalCompletionRate = 100;
                }
                return (float)Math.Round(totalCompletionRate, 3);//取值3位小数
            }
        }

        /// <summary>
        /// 下载速率
        /// </summary>
        public string DownloadSpeedRate { get; private set; } = "0kib/s";

        #region 静态方法

        /// <summary>
        /// 启动一个任务
        /// </summary>
        internal static void StartTask(DownloadTask downloadTask)
        {
            //创建一个任务的所有线程
            downloadTask.CreateAllThread(downloadTask.ID);
            //获得线程下载大小
            long eachThreadShouldDownloadSize =
                General_DownloadManagement.SplitSize(downloadTask.Target.MaxDownloadThread, downloadTask.Url, out long remaining);
            //初始化所有线程
            downloadTask.InitializationAllThread(downloadTask.ID.ToString(), eachThreadShouldDownloadSize, remaining);
            //启动所有线程
            downloadTask.StartAllDownloadThread();
        }

        /// <summary>
        /// 收尾一个任务
        /// </summary>
        internal static void EndTask(DownloadTask downloadTask)
        {
            downloadTask.State = DownloadTaskState.Completed;
            downloadTask.Target.DownloadingTask = downloadTask.Target.DownloadingTask - 1;//下载完毕减少正在下载的任务
            if (downloadTask.Target.WaitingTask > 0)//如果还有等待的线程
            {
                downloadTask.Target.CompletedTask++;
                downloadTask.Target.StartAllocate();//启动分配线程
            }
        }

        #endregion 静态方法

        /// <summary>
        /// 创建一个DownloadTask(不包括ID)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="path"></param>
        public static DownloadTask Create(string url, string path, MultiDownload target)
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

        /// <summary>
        /// 检查DownloadTask是否正确(不包括TaskID)
        /// </summary>
        /// <returns></returns>
        public static bool Check(DownloadTask downloadTask)
        {
            if (NetWorkHelp.CanConnectionUrlAsync(downloadTask.Url).GetAwaiter().GetResult() == true
                && PathHelp.IsRightForRegex(downloadTask.Path))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public DownloadTask(MultiDownload target)
        {
            this.Target = target;
        }

        /// <summary>
        /// 创建所有线程
        /// </summary>
        /// <param name="taskIndex">任务索引</param>
        /// <param name="eachThreadShouldDownloadSize">每个线程应该下载大小</param>
        /// <param name="remainingSize">剩余大小</param>
        public void CreateAllThread(int taskIndex)
        {
            this.Threads = new List<DownloadThread>();//新建列表
            //遍历所有线程
            for (int i = 0; i < this.Target.MaxDownloadThread; i++)
            {
                this.Threads.Add(new DownloadThread());//创建线程数据类并添加
                int index = i;//将index存储在另一个变量，以防止越界
                this.Threads[i].Thread = new Thread(() => this.Target.SendDownloadFileRequestByHttp(taskIndex, this.Threads[index].ID));//将新建线程的赋值给属性
                //将线程设为背景线程
                this.Threads[i].Thread.IsBackground = true;
            }
        }

        //TODO: 重构此方法
        public void InitializationAllThread(string tag, long eachThreadShouldDownloadSize, long remainingSize)
        {
            //将Tag赋值
            this.Tag = tag;

            //检查参数
            if (eachThreadShouldDownloadSize != 0)//判断eachThreadShouldDownloadSize是否不等于0
            {//是
                //遍历所有线程
                for (int i = 0; i < this.Target.MaxDownloadThread; i++)
                {
                    //如果是最后的线程
                    if (i == this.Target.MaxDownloadThread - 1)
                    {
                        //EachThreadShouldDownloadSize = 每个线程应该下载大小 + 剩余大小
                        this.Threads[i].EachThreadShouldDownloadSize = eachThreadShouldDownloadSize + remainingSize - 1;//- 1 的原因是为了给坐标让位
                    }
                    else
                    {
                        //EachThreadShouldDownloadSize = 每个线程应该下载大小
                        this.Threads[i].EachThreadShouldDownloadSize = eachThreadShouldDownloadSize - 1;//- 1 的原因是为了给坐标让位
                    }
                    //将ID赋值
                    this.Threads[i].ID = i;

                    //获得位置
                    long[] positions = General_DownloadManagement.SplitePosition(this.Target, eachThreadShouldDownloadSize);
                    //将位置赋值给线程
                    this.Threads[i].DownloadPosition = positions[i];
                    //将线程的下载路径赋值给线程
                    this.Threads[i].Path = General_DownloadManagement.SplitPath(this.Target, this.Path, tag)[i];
                }
            }
            else
            {//否
                throw new UrlFileFileSizeIsNullOrZeroException(this.Url);//抛出错误
            }
        }

        /// <summary>
        /// 启动所有下载线程
        /// </summary>
        /// <returns></returns>
        public void StartAllDownloadThread()
        {
            //遍历所有线程数据
            foreach (DownloadThread threadData in this.Threads)
            {
                //将获取的线程数据中的线程启动
                threadData.Thread.Start();
            }
            this.StartTimer();//启动计时器
            this.RefreshPath();
            new FileStream(this.Path, FileMode.Create).Close();//创建最终文件流文件
        }

        /// <summary>
        /// 启动运行计时器
        /// </summary>
        public void StartTimer()
        {
            this.downloadTaskTimer.Elapsed += new ElapsedEventHandler(DownloadTaskPerSecond);//绑定时间事件
            this.downloadTaskTimer.Start();//开始计时
        }

        private void DownloadTaskPerSecond(object sender, ElapsedEventArgs e)
        {
            this.DownloadSpeedPerSecond();//计算每秒下载速度
            if (this.Refresh != null)//有方法绑定此事件才Invoke，从而修复空调用异常
            {
                this.Refresh.Invoke(this, new DownloadDataEventArgs(this));//触发事件
            }
        }

        private void DownloadSpeedPerSecond()
        {
            long gap = this.theSecondDownloadSize - this.lastSecondDownloadSize;//算出差距
            this.DownloadSpeedRate = gap.ToDownloadRate();
            this.lastSecondDownloadSize = this.theSecondDownloadSize;
        }

        /// <summary>
        /// 将此任务的所有(包括线程)关闭
        /// </summary>
        public void CloseTask()
        {
            this.CloseAllThread();
            this.downloadTaskTimer.Close();
        }

        /// <summary>
        /// 仅将此任务的线程关闭
        /// </summary>
        public void CloseAllThread()
        {
            //遍历所有线程数据
            foreach (DownloadThread threadData in this.Threads)
            {
                //将获取的线程数据中的线程关闭
                threadData.IsAlive = false;
            }
        }

        /// <summary>
        /// 取消此任务
        /// </summary>
        public void Cancel()
        {
            this.State = DownloadTaskState.Cancelled;
            CloseTask();
        }

        internal void InvokeStateChangeEvent(string name, string url, string path, DownloadTaskState state)
        {
            if (this.StateChange != null)
            {
                DownloadDataEventArgs eventArgs = new DownloadDataEventArgs()
                {
                    Name = name,
                    Url = url,
                    Path = path,
                    State = state
                };
                this.StateChange.Invoke(this, eventArgs);//触发事件
            }
        }

        /// <summary>
        /// 刷新文件路径
        /// </summary>
        private void RefreshPath()
        {
            if (File.Exists(this.Path))
            {
                this.Path = PathHelp.AutomaticFileName(this.Path);
            }
        }
    }
}