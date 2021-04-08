using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using HttpDownloadEngine.Help;
using System.IO;
using System.Net;

namespace HttpDownloadEngine
{

    public enum DownloadType
    {
        HTTP
    }

    public class Download
    {
        public List<DownloadTask> Tasks { get; set; } = new List<DownloadTask>();

        /// <summary>
        /// 最大下载任务数量
        /// </summary>
        public int MaxDownloadTask { get; private set; }

        /// <summary>
        /// 需要下载的任务数量
        /// </summary>
        public int NeedDownloadTask { get; private set; }

        /// <summary>
        /// 最大下载线程数
        /// </summary>
        public int MaxDownloadThread { get; private set; }

        /// <summary>
        /// 在下载在的任务
        /// </summary>
        public int DownloadTask { get; set; }

        /// <summary>
        /// 等待的任务
        /// </summary>
        public int WaitTask { get; set; }

        /// <summary>
        /// 任务索引(每当有任务开始下载此属性则会+1(例如:[任务索引] = 0 => 开始下载 => [任务索引] = 1;))
        /// </summary>
        internal int TaskIndex { get; set; }

        /// <summary>
        /// 控制分配线程的运行(是否杜塞)
        /// </summary>
        private ManualResetEvent AllocateThreadRunningControl { get; set; }

        public Download(int maxRunningTask,int maxDownloadThread)
        {
            this.MaxDownloadTask = maxRunningTask;
            this.MaxDownloadThread = maxDownloadThread;
        }

        public void Add(string url,string path)
        {
            //获取下载路径
            string downloadPath = FileHelp.AutomaticFileName(Path.Combine(path + Path.GetFileName(url)));
            DownloadTask downloadTask = new DownloadTask();
            downloadTask.Path = downloadPath;
            downloadTask.Type = DownloadType.HTTP;
            downloadTask.Url = url;
            this.Tasks.Add(downloadTask);//添加任务
            this.AllocateThreadRunningControl.Set();//发出运行信号

        }

        private void Allocate()
        {
            while (true)
            {
                AllocateThreadRunningControl.WaitOne();//等待运行信号(停止运行)
                if (this.DownloadTask < this.MaxDownloadTask)
                {
                    this.Tasks[this.TaskIndex].CreateAllThread(this, this.TaskIndex);//创建所有线程
                    int eachThreadShouldDownloadSize = this.SplitSize(this.Tasks[this.TaskIndex].Url, out int remaining);//获得线程下载大小
                    this.Tasks[this.TaskIndex].InitializationAllThread(this, eachThreadShouldDownloadSize, remaining);//初始化所有线程
                    this.TaskIndex = this.Tasks[this.TaskIndex].StartAllThread(this);//启动所有线程
                    if (this.WaitTask > 0)//如果等待的任务大于0
                    {
                        this.WaitTask--;//等待的任务减少
                    }
                }
                else
                {
                    this.WaitTask++;//等待的任务增加
                }
                AllocateThreadRunningControl.Reset();//取消运行信号
            }
        }

        /// <summary>
        /// 分割文件
        /// </summary>
        /// <param name="remainingSize">剩余的大小(除完后的余数)</param>
        /// <returns>每个线程应该下载的大小</returns>
        private int SplitSize(string url,out int remainingSize)
        {
            long fileSize = NetWorkHelp.GetUrlFileSize(DownloadType.HTTP,url);

            //每一个线程应当下载的大小
            int eachThreadShouldDownloadSize = ((int)fileSize / this.MaxDownloadThread) - 1;//文件大小/最大下载线程数
            remainingSize = (int)fileSize % this.MaxDownloadThread;
            return eachThreadShouldDownloadSize;
        }

        /// <summary>
        /// 将下载路径分割
        /// </summary>
        /// <param name="path">路径(包含文件)</param>
        /// <returns></returns>
        internal string[] SplitPath(string path)
        {
            string[] paths = new string[this.MaxDownloadThread];
            for (int i = 0; i < MaxDownloadThread; i++)
            {
                //文件名(不含扩展名)
                string name = Path.GetFileName(path).Substring(0, Path.GetFileName(path).LastIndexOf('.'));
                //文件扩展名
                string suffix = ".Download";
                //将计算好的路径赋值
                paths[i] = Path.Combine(Path.GetFullPath(path), $"{name}{suffix}");
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
            int CompletedSizeCount = 0;
            do
            {
                //读取数据
                readBytesCount = responseStream.Read(bytes, 0, bytes.Length);
                //写入数据
                DownloadFileStream.Write(bytes, 0, readBytesCount);

                CompletedSizeCount += readBytesCount;//将下载数量相加
                //将完成率赋值给线程完成率
                this.Tasks[taskIndex].Threads[threadID].CompletionRate = Math.Round((CompletedSizeCount / webResponse.ContentLength) * 100D, 2);//算出完成率
            }
            while (readBytesCount > 0 && this.Tasks[taskIndex].Threads[threadID].IsAlive == true);//是否到达文件流结尾(readBytesCount等于0就是到达文件流结尾)
            if(this.Tasks[taskIndex].Threads[threadID].IsAlive == true)//判断是否线程是否已被设为"不工作"
            {//否
                DownloadFileStream.Flush();//释放所有数据
                DownloadFileStream.Close();//解除对文件的占用
                this.Tasks[taskIndex].CompleteDownloadThreadCount++;//完成下载的线程数量增加

                if (this.Tasks[taskIndex].CompleteDownloadThreadCount == this.MaxDownloadTask)//是否所有线程完成下载
                {
                    //开始合并文件
                    this.Combine(taskIndex, threadID);
                }
            }
            else
            {//是
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
            //读取缓存
            byte[] bytes = new byte[4096];//读取的数据将存放在此后写入文件
            finalFileStream = new FileStream(this.Tasks[taskIndex].Path, FileMode.Create);
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
        }
    }

    public class DownloadTask
    {
        public string Url { get; internal set; }

        public DownloadType Type { get; internal set; }

        public string Path { get; internal set; }

        /// <summary>
        /// 完成下载的线程数量
        /// </summary>
        public byte CompleteDownloadThreadCount { get; internal set; }

        public bool IsAlive { get; internal set; }

        public List<DownloadThread> Threads { get; internal set; }

        /// <summary>
        /// 完成率
        /// </summary>
        public double CompletionRate
        {
            get
            {
                double totalCompletionRate = 0;
                //遍历所有下载线程
                foreach (DownloadThread thread in this.Threads)
                {
                    totalCompletionRate += thread.CompletionRate;
                }
                return totalCompletionRate;
            }
        }

        /// <summary>
        /// 创建所有线程
        /// </summary>
        /// <param name="download">下载类</param>
        /// <param name="taskIndex">任务索引</param>
        /// <param name="eachThreadShouldDownloadSize">每个线程应该下载大小</param>
        /// <param name="remainingSize">剩余大小</param>
        public void CreateAllThread(Download download, int taskIndex)
        {
            //遍历所有线程
            for (int i = 0; i < download.MaxDownloadThread; i++)
            {
                //判断类型
                switch (this.Type)
                {
                    //如果是HTTP
                    case DownloadType.HTTP:
                        //将新建线程的赋值给属性
                        this.Threads[i].Thread = new Thread(() => download.HttpDownload(taskIndex,this.Threads[i].ID));
                        //将线程设为背景线程
                        this.Threads[i].Thread.IsBackground = true;
                        break;
                    default:
                        throw new NullReferenceException();
                }
            }
        }

        public void InitializationAllThread(Download download, int eachThreadShouldDownloadSize, int remainingSize)
        {
            //遍历所有线程
            for (int i = 0; i < download.MaxDownloadThread; i++)
            {
                //如果是最后的线程
                if (i == download.MaxDownloadThread - 1)
                {
                    //EachThreadShouldDownloadSize = 每个线程应该下载大小 + 剩余大小
                    this.Threads[i].EachThreadShouldDownloadSize = eachThreadShouldDownloadSize + remainingSize;
                }
                else
                {
                    //EachThreadShouldDownloadSize = 每个线程应该下载大小
                    this.Threads[i].EachThreadShouldDownloadSize = eachThreadShouldDownloadSize;
                }

                //将ID赋值
                this.Threads[i].ID = i;

                //获得位置
                int[] positions = download.SplitePosition(eachThreadShouldDownloadSize);
                //将位置赋值给线程
                this.Threads[i].DownloadPosition = positions[i];
                //将线程的下载路径赋值给线程
                this.Threads[i].Path = download.SplitPath(this.Path)[i];
            }
        }

        public int StartAllThread(Download download)
        {
            //遍历所有线程数据
            foreach (DownloadThread threadData in this.Threads)
            {
                //将获取的线程数据中的线程启动
                threadData.Thread.Start();
            }
            download.TaskIndex++;
            //返回任务索引
            return download.TaskIndex++;
        }

        public void CloseAllThread()
        {
            //遍历所有线程数据
            foreach (DownloadThread threadData in this.Threads)
            {
                //将获取的线程数据中的线程关闭
                threadData.IsAlive = false;
            }
        }

    }

    public class DownloadThread
    {
        public int ID { get; set; }

        public string Path { get; set; }

        /// <summary>
        /// 每一个线程应当下载的大小
        /// </summary>
        public int EachThreadShouldDownloadSize { get;internal  set; }

        /// <summary>
        /// 线程应当下载的开始位置
        /// </summary>
        public int DownloadPosition { get; internal set; }

        /// <summary>
        /// 下载线程是否存活
        /// </summary>
        public bool IsAlive { get; set; }

        public Thread Thread { get; set; }

        /// <summary>
        /// 完成率
        /// </summary>
        public double CompletionRate { get; internal set; }

    }
}
