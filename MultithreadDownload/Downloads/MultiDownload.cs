using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using MultithreadDownload.Help;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using MultithreadDownload.Exceptions;
using MultithreadDownload.Events;
using System.Web;
using System.Drawing;
using System.Net.Http.Headers;
using MultithreadDownload.Ways;

namespace MultithreadDownload.Downloads
{

    public class MultiDownload
    {
        public List<DownloadTask> Tasks { get; internal set; } = new List<DownloadTask>();

        internal HyperTextTransferProtocol_DownloadManagement Http_DownloadManagement { get; private set; }

        /// <summary>
        /// 最大下载中任务数量
        /// </summary>
        public readonly int MaxDownloadingTask;

        /// <summary>
        /// 最大下载线程数
        /// </summary>
        public readonly int MaxDownloadThread;

        /// <summary>
        /// 完成下载的任务数量
        /// </summary>
        public int CompletedTask { get; internal set; }

        /// <summary>
        /// 正在下载的任务
        /// </summary>
        public int DownloadingTask { get; internal set; }

        /// <summary>
        /// 等待的任务
        /// </summary>
        public int WaitingTask { get; internal set; }

        /// <summary>
        /// 当链接的文件大小为零或NULL时是否以单线程状态下载(未完成)
        /// </summary>
        //public bool WhenFileSizeIsNullOrZeroShouldDownload { get; set; }


        /// <summary>
        /// 下一个要下载的任务的ID (每当有任务开始下载此属性则会+1(例如:[任务索引] = 0 => 开始下载 => [任务索引] = 1;))
        /// </summary>
        internal int NextShouldDownloadTaskIndex { get; set; }

        /// <summary>
        /// 控制分配线程的运行(是否杜塞)
        /// </summary>
        private ManualResetEvent AllocateThreadRunningControl { get; set; }

        public DownloadTask this[int index]
        {
            get
            {
                return this.Tasks[index];
            }

            set
            {
                this.Tasks[index] = value;
            }
        }


        public MultiDownload(int maxDownloadingTask, int maxDownloadThread)
        {
            this.MaxDownloadingTask = maxDownloadingTask;
            this.MaxDownloadThread = maxDownloadThread;
            this.Http_DownloadManagement = new HyperTextTransferProtocol_DownloadManagement(this);
            this.AllocateThreadRunningControl = new ManualResetEvent(false);//初始化变量
            Thread allocateThread = new Thread(this.Allocate) { IsBackground = true};//创建用于分配下载任务的线程
            allocateThread.Start();//启动线程
        }

        #region 公开方法——辅助性
        /// <summary>
        /// 是否有任务等待分配的状态
        /// </summary>
        /// <returns>是否</returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public bool IsTaskWaitForAllocated()
        {
            if (this.DownloadingTask + this.WaitingTask + this.CompletedTask == this.Tasks.Count)
            {
                return false;
            }
            else if (this.DownloadingTask + this.WaitingTask + this.CompletedTask < this.Tasks.Count)
            {
                return true;
            }
            else
            {
                throw new ArgumentOutOfRangeException(
                    $"计数器过界,任务总和不应大于任务总数，却大于" +
                    $"——D:{this.DownloadingTask} W:{this.WaitingTask} C{this.CompletedTask} Count:{this.Tasks.Count}");
            }
        }

        public bool IsTaskWaitForDownload()
        {
            if (this.WaitingTask > 0)
            {
                return true;
            }
            else if (this.WaitingTask == 0)
            {
                return false;
            }
            else
            {
                throw new ArgumentOutOfRangeException(
                    $"计数器过界,等待任务数不应小于0，却小于" +
                    $"——D:{this.DownloadingTask} W:{this.WaitingTask} C{this.CompletedTask} Count:{this.Tasks.Count}");
            }
        }

        /// <summary>
        /// 是否下载中任务达上限
        /// </summary>
        /// <returns></returns>
        public bool IsDownloadingTaskFull()
        {
            if (this.DownloadingTask < this.MaxDownloadingTask)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion


        /// <summary>
        /// 添加下载任务
        /// </summary>
        /// <param name="url">HTTP链接</param>
        /// <param name="path">存放路径</param>
        /// <returns>下载任务编号</returns>
        /// <exception cref="UrlCanNotConnectionException">链接无法链接</exception>
        public int Add(string url, string path)
        {
            DownloadTask task = DownloadTask.Create(url, path, this);//新建DownloadTask
            //任务编号等于列表的项目数量 (因为从0开始)
            task.ID = this.Tasks.Count;
            this.Tasks.Add(task);//添加任务
            this.WaitingTask++;//等待的任务增加
            this.StartAllocate();//发出运行信号
            return task.ID;
        }

        private void Allocate()
        {
            while (true)
            {
                this.WaitAllocate();//等待运行信号(停止运行)
                //是否下载中任务没有达上限且有任务等待下载
                if (!IsDownloadingTaskFull() && IsTaskWaitForDownload())
                {
                    //Tasks列表中项目的最大索引是否小于下一个要下载的任务的ID => 防止越界
                    if (this.Tasks.Count - 1 < NextShouldDownloadTaskIndex)
                    {
                        Debug.WriteLine($"IndexOutOfRangeException异常——" +
                            $"最大索引:{this.Tasks.Count - 1} 下一个要下载的任务的ID:{NextShouldDownloadTaskIndex}");
                        throw new IndexOutOfRangeException("Tasks列表中项目的最大索引小于下一个要下载的任务的ID");
                    }
                    //启动任务
                    DownloadTask.StartTask(this.Tasks[NextShouldDownloadTaskIndex]);

                    this.NextShouldDownloadTaskIndex = ++this.NextShouldDownloadTaskIndex;
                    //下载中的任务增加
                    this.DownloadingTask++;
                    //如果有等待下载的任务
                    if (this.IsTaskWaitForDownload())
                    {
                        this.WaitingTask--;//等待的任务减少
                    }
                }

                //是否没有任务等待分配
                if (!IsTaskWaitForAllocated())
                {
                    AllocateThreadRunningControl.Reset();//取消运行信号
                }
            }
        }

        internal void StartAllocate()
        {
            this.AllocateThreadRunningControl.Set();//发出运行信号
        }

        internal void StopAllocate()
        {
            this.AllocateThreadRunningControl.Reset();//取消运行信号
        }

        internal void WaitAllocate()
        {
            this.AllocateThreadRunningControl.WaitOne();//等待运行信号(停止运行)
        }

        /// <summary>
        /// 使用Http协议下载文件
        /// </summary>
        /// <param name="taskIndex">任务编号</param>
        /// <param name="threadID">线程编号</param>
        /// <returns></returns>
        internal void SendDownloadFileRequestByHttp(int taskIndex, int threadID)
        {
            //新建DownloadTaskThreadInfo对象
            DownloadTaskThreadInfo taskThreadInfo = new DownloadTaskThreadInfo(this, taskIndex, threadID);
            //得到响应流
            Stream responseStream = Http_DownloadManagement.GetStream(taskThreadInfo);
            if(responseStream == null)
            {
                Debug.WriteLine($"线程{taskThreadInfo.ThreadID} 请求失败");
                this.Tasks[taskThreadInfo.TaskID].Cancel();
                return;
            }
            Debug.WriteLine($"线程{taskThreadInfo.ThreadID}尝试访问{taskThreadInfo.Path}");
            try
            {
                //新建文件流用于操作文件
                FileStream downloadFileStream = new FileStream(taskThreadInfo.Path, FileMode.Create);
                Debug.WriteLine($"线程{taskThreadInfo.ThreadID} 开始下载");
                this.Tasks[taskThreadInfo.TaskID].State = DownloadTaskState.Downloading;
                int readBytesCount = 0;
                do
                {
                    readBytesCount = Http_DownloadManagement.DownloadFile(responseStream, downloadFileStream, taskThreadInfo);
                    //Debug.WriteLine($"线程{taskThreadInfo.ThreadID} CompletedSize:{readBytesCount}");
                    if (readBytesCount == -1)
                    {
                        Debug.WriteLine($"下载任务失败，因为于线程{taskThreadInfo.ThreadID}中readBytesCount为-1");
                        this.Tasks[taskThreadInfo.TaskID].Cancel();
                        return;
                    }
                    Http_DownloadManagement.CaculateCompleteNumber(taskThreadInfo, readBytesCount);
                }
                while (readBytesCount > 0 && this.Tasks[taskThreadInfo.TaskID].State == DownloadTaskState.Downloading);//是否到达文件流结尾(readBytesCount等于0就是到达文件流结尾)
                Http_DownloadManagement.PostDownload(downloadFileStream, taskThreadInfo);
            }
            catch (UnauthorizedAccessException)
            {
                Debug.WriteLine($"线程{taskThreadInfo.ThreadID} 无法访问{taskThreadInfo.Path}");
                this.Tasks[taskThreadInfo.TaskID].Cancel();
            }
            catch (Exception)
            {
                Debug.WriteLine($"线程{taskThreadInfo.ThreadID} 未知错误");
                this.Tasks[taskThreadInfo.TaskID].Cancel();
            }
        }
    }

    public class DownloadTaskThreadInfo
    {
        public MultiDownload MultiDownloadObject { get; set; }
        public int TaskID { get; set; }
        public int ThreadID { get; set; }
        public string Url { get; set; }
        public string Path { get; set; }
        public long DownloadPosition { get; set; }
        public long EachThreadShouldDownloadSize { get; set; }

        public DownloadTaskThreadInfo(MultiDownload multiDownloadObject ,int taskID,int threadID)
        {
            try
            {
                MultiDownloadObject = multiDownloadObject;
                this.TaskID = taskID;
                this.ThreadID = threadID;
                this.Url = this.MultiDownloadObject.Tasks[taskID].Url;
                this.Path = this.MultiDownloadObject.Tasks[taskID].Threads[threadID].Path;
                this.DownloadPosition = this.MultiDownloadObject.Tasks[taskID].Threads[this.ThreadID].DownloadPosition;
                this.EachThreadShouldDownloadSize = this.MultiDownloadObject.Tasks[taskID].Threads[this.ThreadID].EachThreadShouldDownloadSize;
            }
            catch (IndexOutOfRangeException)
            {
                Debug.WriteLine($"异常：IndexOutOfRangeException 对于  taskID为 {taskID} 且 threadID为 {threadID}");
                throw;
            }
        }
    }
}
