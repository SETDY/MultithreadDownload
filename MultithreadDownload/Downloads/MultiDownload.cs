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

namespace MultithreadDownload.Downloads
{

    public class MultiDownload
    {
        public List<DownloadTask> Tasks { get; internal set; } = new List<DownloadTask>();

        /// <summary>
        /// 重试次数
        /// </summary>
        private const uint TRYTIMES = 5;

        /// <summary>
        /// 重试等待时间
        /// </summary>
        private const uint WAITTIMES = 5000;

        /// <summary>
        /// 最大下载中任务数量
        /// </summary>
        public int MaxDownloadingTask { get; private set; }

        /// <summary>
        /// 完成下载的任务数量
        /// </summary>
        public int CompleteTask { get; private set; }

        /// <summary>
        /// 最大下载线程数
        /// </summary>
        public int MaxDownloadThread { get; private set; }

        /// <summary>
        /// 正在下载的任务
        /// </summary>
        public int DownloadingTask { get; private set; }

        /// <summary>
        /// 等待的任务
        /// </summary>
        public int WaitTask { get; private set; }

        /// <summary>
        /// 当链接的文件大小为零或NULL时是否以单线程状态下载(未完成)
        /// </summary>
        //public bool WhenFileSizeIsNullOrZeroShouldDownload { get; set; }


        /// <summary>
        /// 任务索引(每当有任务开始下载此属性则会+1(例如:[任务索引] = 0 => 开始下载 => [任务索引] = 1;))
        /// </summary>
        internal int TaskIndex { get; set; }

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
            this.AllocateThreadRunningControl = new ManualResetEvent(false);//初始化变量
            Thread allocateThread = new Thread(this.Allocate) { IsBackground = true};//创建用于分配下载任务的线程
            allocateThread.Start();//启动线程
        }

        /// <summary>
        /// 添加下载任务
        /// </summary>
        /// <param name="url">HTTO链接</param>
        /// <param name="path">存放路径</param>
        /// <returns>下载任务编号</returns>
        /// <exception cref="UrlCanNotConnectionException">链接无法链接</exception>
        public int Add(string url, string path)
        {
            DownloadTask task = DownloadTask.Create(url, path, this);//新建DownloadTask
            task.ID = TaskIndex;
            this.Tasks.Add(task);//添加任务
            this.AllocateThreadRunningControl.Set();//发出运行信号
            return task.ID;
        }

        private void Allocate()
        {
            while (true)
            {
                AllocateThreadRunningControl.WaitOne();//等待运行信号(停止运行)
                if (this.DownloadingTask < this.MaxDownloadingTask && this.WaitTask >= 0)
                {
                    this.Tasks[this.TaskIndex].CreateAllThread(this.TaskIndex);//创建所有线程
                    long eachThreadShouldDownloadSize = this.SplitSize(this.Tasks[this.TaskIndex].Url, out long remaining);//获得线程下载大小
                    this.Tasks[this.TaskIndex].InitializationAllThread(this.TaskIndex.ToString(), eachThreadShouldDownloadSize, remaining);//初始化所有线程
                    this.TaskIndex = this.Tasks[this.TaskIndex].StartAllThread();//启动所有线程
                    this.DownloadingTask++;
                    if (this.WaitTask > 0)//如果等待的任务大于0
                    {
                        this.WaitTask--;//等待的任务减少
                    }
                }
                else
                {
                    this.WaitTask++;//等待的任务增加
                }
                if(this.DownloadingTask + this.WaitTask + this.CompleteTask == this.Tasks.Count)
                {
                    AllocateThreadRunningControl.Reset();//取消运行信号
                }
            }
        }

        /// <summary>
        /// 分割文件
        /// </summary>
        /// <param name="remainingSize">剩余的大小(除完后的余数)</param>
        /// <returns>每个线程应该下载的大小</returns>
        private long SplitSize(string url, out long remainingSize)
        {
            long fileSize = NetWorkHelp.GetUrlFileSizeAsync(url).GetAwaiter().GetResult();

            //每一个线程应当下载的大小
            long eachThreadShouldDownloadSize = (long)fileSize / this.MaxDownloadThread;//文件大小/最大下载线程数
            remainingSize = (long)fileSize % this.MaxDownloadThread;//取余数
            return eachThreadShouldDownloadSize;
        }

        /// <summary>
        /// 将下载路径分割
        /// </summary>
        /// <param name="path">路径(包含文件)</param>
        /// <returns></returns>
        internal string[] SplitPath(string path,string tag)
        {
            string[] paths = new string[this.MaxDownloadThread];
            for (int i = 0; i < MaxDownloadThread; i++)
            {
                //目标文件名(不含扩展名)
                string targetFileNameWE = Path.GetFileNameWithoutExtension(path);
                //临时文件名文件扩展名
                string extension = ".Download";
                //将计算好的路径赋值
                paths[i] = Path.Combine(Path.GetPathRoot(path), $"{targetFileNameWE} [{tag}]-{i}{extension}");
            }
            return paths;
        }


        /// <summary>
        /// 分割文件位置
        /// </summary>
        /// <param name="eachThreadShouldDownloadSize"></param>
        /// <returns>位置</returns>
        internal long[] SplitePosition(long eachThreadShouldDownloadSize)
        {
            long[] reutnPosition = new long[this.MaxDownloadThread];
            for (int i = 0; i < this.MaxDownloadThread; i++)
            {
                //获得文件在下载式因在哪一出位置开始下载并赋值
                reutnPosition[i] = eachThreadShouldDownloadSize * i;
            }
            return reutnPosition;
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
            Stream responseStream = this.GetStream(taskThreadInfo);
            if(responseStream == null)
            {
                Debug.WriteLine($"线程{taskThreadInfo.ThreadID} 请求失败");
                this.Tasks[taskThreadInfo.TaskID].Cancel();
                return;
            }
            //新建文件流用于操作文件
            FileStream downloadFileStream = new FileStream(taskThreadInfo.Path, FileMode.Create);
            Debug.WriteLine($"线程{taskThreadInfo.ThreadID} 开始下载");
            this.Tasks[taskThreadInfo.TaskID].State = DownloadTaskState.Downloading;
            int readBytesCount = 0;
            do
            {
                readBytesCount = this.DownloadFile(responseStream, downloadFileStream, taskThreadInfo);
                //Debug.WriteLine($"线程{taskThreadInfo.ThreadID} CompletedSize:{readBytesCount}");
                if (readBytesCount == -1)
                {
                    Debug.WriteLine($"下载任务失败，因为于线程{taskThreadInfo.ThreadID}中readBytesCount为-1");
                    this.Tasks[taskThreadInfo.TaskID].Cancel();
                    return;
                }
                this.CaculateCompleteNumber(taskThreadInfo, readBytesCount);
            }
            while (readBytesCount > 0 && this.Tasks[taskThreadInfo.TaskID].State == DownloadTaskState.Downloading);//是否到达文件流结尾(readBytesCount等于0就是到达文件流结尾)
            this.PostDownload(downloadFileStream, taskThreadInfo);
        }

        private Stream GetStream(DownloadTaskThreadInfo taskThreadInfo)
        {
            //新建HttpClient对象
            HttpClient httpClient = new HttpClient();
            //新建Http请求信息
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, taskThreadInfo.Url);
            //添加请求文件时的偏移(文件请求范围)
            httpRequestMessage.Headers.Range = new RangeHeaderValue(taskThreadInfo.DownloadPosition, 
                taskThreadInfo.DownloadPosition + taskThreadInfo.EachThreadShouldDownloadSize);
            try
            {
                Debug.WriteLine($"线程{taskThreadInfo.ThreadID} 开始请求");
                //发送请求并获得Http回应
                HttpResponseMessage response = httpClient.Send(httpRequestMessage);
                Debug.WriteLine($"线程{taskThreadInfo.ThreadID} 请求成功");
                //检查请求是否成功
                response.EnsureSuccessStatusCode();
                //赋值响应流
                Stream responseStream = response.Content.ReadAsStream();
                return responseStream;
            }
            catch (Exception)
            {
                return null;
            }

        }

        private int DownloadFile(Stream responseStream, FileStream downloadFileStream, DownloadTaskThreadInfo taskThreadInfo)
        {
            //读取的字节数
            int readBytesCount = 0;
            //读取缓存
            byte[] bytes = new byte[4096];//读取的数据将存放在此后写入文件
                                          //失败尝试次数
            short tryCount = 0;

            do
            {
                try
                {
                    //如果尝试次数大于或等于TRYTIMES
                    if (tryCount >= TRYTIMES)
                    {
                        Debug.WriteLine($"线程[{taskThreadInfo.ThreadID}] 5次尝试请求并读取数据失败，该下载任务取消");
                        this.Tasks[taskThreadInfo.TaskID].Cancel();//取消该任务
                        break;
                    }
                    //读取数据
                    readBytesCount = responseStream.Read(bytes, 0, bytes.Length);
                }
                catch (Exception)
                {
                    Debug.WriteLine($"线程[{taskThreadInfo.ThreadID}] 读取数据失败，等待{WAITTIMES / 1000}秒后进行第{tryCount + 1}次重试");
                    tryCount++;
                    Thread.Sleep((int)WAITTIMES);//等待WAITTIMES1
                }
            }
            while (tryCount != 0);

            //下载状态是否为取消
            if (this.Tasks[taskThreadInfo.TaskID].State == DownloadTaskState.Cancelled)// 判断状态是否为取消
            {
                downloadFileStream.Flush();//释放所有数据
                downloadFileStream.Close();//解除对文件的占用
                File.Delete(taskThreadInfo.Path);//删除下载的文件
                return -1;//返回
            }

            //写入数据
            downloadFileStream.Write(bytes, 0, readBytesCount);

            //返回下载字节数量
            return readBytesCount;
        }

        private void CaculateCompleteNumber(DownloadTaskThreadInfo taskThreadInfo,int readBytesCount)
        {
            this.Tasks[taskThreadInfo.TaskID].Threads[taskThreadInfo.ThreadID].CompletedSizeCount += readBytesCount;//将下载数量相加
                                                                                                                    //将完成率赋值给线程完成率
            this.Tasks[taskThreadInfo.TaskID].Threads[taskThreadInfo.ThreadID].CompletionRate =
                (float)Math.Round(((float)this.Tasks[taskThreadInfo.TaskID].Threads[taskThreadInfo.ThreadID].CompletedSizeCount / this.Tasks[taskThreadInfo.TaskID].
                Threads[taskThreadInfo.ThreadID].EachThreadShouldDownloadSize) * 100F, 2);//算出完成率
        }

        private void PostDownload(FileStream downloadFileStream, DownloadTaskThreadInfo taskThreadInfo)
        {
            if (this.Tasks[taskThreadInfo.TaskID].Threads[taskThreadInfo.ThreadID].IsAlive == true)//判断是否线程是否被设为"工作"
            {//是
                downloadFileStream.Flush();//释放所有数据
                downloadFileStream.Close();//解除对文件的占用
                lock (this.Tasks[taskThreadInfo.TaskID].DownloadLocker)//保证每次只有一个线程操作
                {
                    this.Tasks[taskThreadInfo.TaskID].CompleteDownloadThreadCount++;//完成下载的线程数量增加
                }

                if (this.Tasks[taskThreadInfo.TaskID].CompleteDownloadThreadCount == this.MaxDownloadThread && new FileInfo((this.Tasks[taskThreadInfo.TaskID].Path)).Length == 0)//是否所有线程完成下载
                {
                    //开始合并文件
                    this.Combine(taskThreadInfo.TaskID, taskThreadInfo.ThreadID);
                }
            }
            else
            {//否
                downloadFileStream.Flush();//释放所有数据
                downloadFileStream.Close();//解除对文件的占用
                File.Delete(taskThreadInfo.Path);//删除下载的文件
                return;//返回
            }
        }

        /// <summary>
        /// 合并文件
        /// </summary>
        /// <param name="taskIndex"></param>
        /// <param name="threadID"></param>
        private void Combine(int taskIndex, int threadID)
            {
            FileStream finalFileStream;//最终文件流
            FileStream tempReadFilrStream;//用于读取临时下载文件的流
            int readBytesCount;//每次读取的字节数
            //读取缓存 //读取的数据将存放在此后写入文件
            byte[] bytes = new byte[1024];//每次的读取的大小(1024最稳,4096最快)
            try
            {
                finalFileStream = new FileStream(this.Tasks[taskIndex].Path, FileMode.Open);
            }
            catch (Exception)
            {
                return;
            }
            //遍历所有下载线程
            foreach (DownloadThread thread in this.Tasks[taskIndex].Threads)
            {
                tempReadFilrStream = new FileStream(thread.Path, FileMode.Open);
                do
                {
                    //读取数据
                    readBytesCount = tempReadFilrStream.Read(bytes, 0, bytes.Length);
                    //写入数据
                    finalFileStream.Write(bytes, 0, readBytesCount);
                }
                while (readBytesCount > 0);//是否到达文件流结尾(readBytesCount等于0就是到达文件流结尾)
                tempReadFilrStream.Flush();//释放所有数据
                tempReadFilrStream.Close();//解除对文件的占用
                File.Delete(thread.Path);//删除文件
            }
            finalFileStream.Flush();//释放所有数据
            finalFileStream.Close();//解除对文件的占

            this.Tasks[taskIndex].State = DownloadTaskState.Completed;
            this.DownloadingTask = this.DownloadingTask - 1;//下载完毕减少正在下载的任务
            if (this.WaitTask > 0)//如果还有等待的线程
            {
                this.CompleteTask++;
                this.AllocateThreadRunningControl.Set();//启动分配线程
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
