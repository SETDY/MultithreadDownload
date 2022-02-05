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

namespace MultithreadDownload.Downloads
{

    public class MultiDownload
    {
        public List<DownloadTask> Tasks { get; internal set; } = new List<DownloadTask>();

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
        /// 在下载在的任务
        /// </summary>
        public int DownloadTask { get; private set; }

        /// <summary>
        /// 等待的任务
        /// </summary>
        public int WaitTask { get; private set; }

        /// <summary>
        /// 当链接的文件大小为零或NULL时是否以单线程状态下载
        /// </summary>
        public bool WhenFileSizeIsNullOrZeroShouldDownload { get; set; }

        /// <summary>
        /// 任务索引(每当有任务开始下载此属性则会+1(例如:[任务索引] = 0 => 开始下载 => [任务索引] = 1;))
        /// </summary>
        internal int TaskIndex { get; set; }

        /// <summary>
        /// 控制分配线程的运行(是否杜塞)
        /// </summary>
        private ManualResetEvent AllocateThreadRunningControl { get; set; }

        public MultiDownload(int maxDownloadingTask, int maxDownloadThread)
        {
            this.MaxDownloadingTask = maxDownloadingTask;
            this.MaxDownloadThread = maxDownloadThread;
            this.AllocateThreadRunningControl = new ManualResetEvent(false);//初始化变量
            Thread allocateThread = new Thread(this.Allocate) { IsBackground = true};//创建用于分配下载任务的线程
            allocateThread.Start();//启动线程
        }

        public void Add(string url,string path, DownloadTaskComplete taskComplete)
        {
            if(NetWorkHelp.CanConnection(url))//链接是否可以连接
            {//是
                //获取下载路径
                string downloadPath = Path.Combine(path + Path.GetFileName(url));
                DownloadTask downloadTask = new DownloadTask(this) { TaskComplete = taskComplete };//新建下载任务
                downloadTask.Path = downloadPath;//将路径赋值
                downloadTask.Url = url;//将链接赋值
                this.Tasks.Add(downloadTask);//添加任务
                this.AllocateThreadRunningControl.Set();//发出运行信号
            }
            else
            {//否
                throw new UrlCanNotConnectionException(url);//抛出连接无法连接错误
            }
        }

        public void Add(string url, string path)
        {
            if (NetWorkHelp.CanConnection(url))//链接是否可以连接
            {//是
                //获取下载路径
                string downloadPath = Path.Combine(path + Path.GetFileName(url));
                DownloadTask downloadTask = new DownloadTask(this);//新建下载任务
                downloadTask.Path = downloadPath;//将路径赋值
                downloadTask.Url = url;//将链接赋值
                this.Tasks.Add(downloadTask);//添加任务
                this.AllocateThreadRunningControl.Set();//发出运行信号
            }
            else
            {//否
                throw new UrlCanNotConnectionException(url);//抛出连接无法连接错误
            }
        }

        private void Allocate()
        {
            while (true)
            {
                AllocateThreadRunningControl.WaitOne();//等待运行信号(停止运行)
                if (this.DownloadTask < this.MaxDownloadingTask && this.WaitTask > 0)
                {
                    this.Tasks[this.TaskIndex].CreateAllThread(this.TaskIndex);//创建所有线程
                    int eachThreadShouldDownloadSize = this.SplitSize(this.Tasks[this.TaskIndex].Url, out int remaining);//获得线程下载大小
                    this.Tasks[this.TaskIndex].InitializationAllThread(this.TaskIndex.ToString(), eachThreadShouldDownloadSize, remaining);//初始化所有线程
                    this.TaskIndex = this.Tasks[this.TaskIndex].StartAllThread();//启动所有线程
                    this.DownloadTask++;
                    if (this.WaitTask > 0)//如果等待的任务大于0
                    {
                        this.WaitTask--;//等待的任务减少
                    }
                }
                else
                {
                    this.WaitTask++;//等待的任务增加
                }
                if(this.DownloadTask + this.WaitTask + this.CompleteTask == this.Tasks.Count)
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
        private int SplitSize(string url,out int remainingSize)
        {
            long fileSize = NetWorkHelp.GetUrlFileSize(url);

            //每一个线程应当下载的大小
            int eachThreadShouldDownloadSize = (int)fileSize / this.MaxDownloadThread;//文件大小/最大下载线程数
            remainingSize = (int)fileSize % this.MaxDownloadThread;//取余数
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
                //文件名(不含扩展名)
                string name = Path.GetFileName(path).Substring(0, Path.GetFileName(path).LastIndexOf('.'));
                //文件扩展名
                string suffix = ".Download";
                //将计算好的路径赋值
                paths[i] = Path.Combine(Path.GetPathRoot(path), $"{name} [{tag}]-{i}{suffix}");
            }
            return paths;
        }


        /// <summary>
        /// 分割文件位置
        /// </summary>
        /// <param name="eachThreadShouldDownloadSize"></param>
        /// <returns>位置</returns>
        internal int[] SplitePosition(int eachThreadShouldDownloadSize)
        {
            int[] reutnPosition = new int[this.MaxDownloadThread];
            for (int i = 0; i < this.MaxDownloadThread; i++)
            {
                //获得文件在下载式因在哪一出位置开始下载并赋值
                reutnPosition[i] = eachThreadShouldDownloadSize * i;
            }
            return reutnPosition;
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="taskIndex"></param>
        /// <param name="threadID"></param>
        internal void HttpDownload(int taskIndex,int threadID)
        {
            string url = this.Tasks[taskIndex].Url;//下载链接
            string path = this.Tasks[taskIndex].Threads[threadID].Path;//下载路径
            int position = this.Tasks[taskIndex].Threads[threadID].DownloadPosition;//下载位置
            int size = this.Tasks[taskIndex].Threads[threadID].EachThreadShouldDownloadSize;//下载大小

            HttpWebRequest httpWebRequest = WebRequest.CreateHttp(url);//新建Http请求
            httpWebRequest.Method = "GET";//将Http请求类型设为GET
            httpWebRequest.AddRange(position, position + size);//添加请求文件时的偏移(文件位置)
            WebResponse webResponse = (WebResponse)httpWebRequest.GetResponse();//发送请求并获得Http回应
            //将存储在响应流
            Stream responseStream = webResponse.GetResponseStream();//获取Http回应中返回的文件
            //新建文件流用于操作文件
            FileStream DownloadFileStream = new FileStream(path, FileMode.Create);
            //读取的字节数
            int readBytesCount = 0;
            //读取缓存
            byte[] bytes = new byte[4096];//读取的数据将存放在此后写入文件
            //完成的大小数量
            do
            {
                //读取数据
                readBytesCount = responseStream.Read(bytes, 0, bytes.Length);
                //写入数据
                DownloadFileStream.Write(bytes, 0, readBytesCount);

                this.Tasks[taskIndex].Threads[threadID].CompletedSizeCount += readBytesCount;//将下载数量相加
                //将完成率赋值给线程完成率
                this.Tasks[taskIndex].Threads[threadID].CompletionRate = (float)Math.Round(((float)this.Tasks[taskIndex].Threads[threadID].CompletedSizeCount / webResponse.ContentLength) * 100F, 2);//算出完成率
                if(this.Tasks[taskIndex].Threads[threadID].IsAlive == false)// 判断是否线程是否被设为"不工作"
                {
                    DownloadFileStream.Flush();//释放所有数据
                    DownloadFileStream.Close();//解除对文件的占用
                    File.Delete(path);//删除下载的文件
                    return;//返回
                }
            }
            while (readBytesCount > 0 && this.Tasks[taskIndex].Threads[threadID].IsAlive == true);//是否到达文件流结尾(readBytesCount等于0就是到达文件流结尾)
            if(this.Tasks[taskIndex].Threads[threadID].IsAlive == true)//判断是否线程是否被设为"工作"
            {//是
                DownloadFileStream.Flush();//释放所有数据
                DownloadFileStream.Close();//解除对文件的占用
                lock(this.Tasks[taskIndex].DownloadLocker)//保证每次只有一个线程操作
                {
                    this.Tasks[taskIndex].CompleteDownloadThreadCount++;//完成下载的线程数量增加
                }
                Console.WriteLine($"{Path.GetFileName(path)} --- {threadID} --- {this.Tasks[taskIndex].CompleteDownloadThreadCount} ");


                if (this.Tasks[taskIndex].CompleteDownloadThreadCount == this.MaxDownloadThread && new FileInfo((this.Tasks[taskIndex].Path)).Length == 0)//是否所有线程完成下载
                {
                    //开始合并文件
                    this.Combine(taskIndex, threadID);
                }
            }
            else
            {//否
                DownloadFileStream.Flush();//释放所有数据
                DownloadFileStream.Close();//解除对文件的占用
                File.Delete(path);//删除下载的文件
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
            finalFileStream.Close();//解除对文件的占用

            this.DownloadTask = this.DownloadTask - 1;//下载完毕减少正在下载的任务
            if(this.WaitTask > 0)//如果还有等待的线程
            {
                this.CompleteTask++;
                this.AllocateThreadRunningControl.Set();//启动分配线程
            }
            if(this.Tasks[taskIndex].TaskComplete != null)
            {
                this.Tasks[taskIndex].TaskComplete.Invoke();//运行任务完成委托
            }        
        }
    }

    public delegate void DownloadTaskComplete();
}
