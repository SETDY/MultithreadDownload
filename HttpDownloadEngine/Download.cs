using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using HttpDownloadEngine.Help;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HttpDownloadEngine
{

    public class Download
    {
        public List<DownloadTask> Tasks { get; internal set; } = new List<DownloadTask>();

        /// <summary>
        /// 最大下载任务数量
        /// </summary>
        public int MaxDownloadTask { get; private set; }

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
                if (this.DownloadTask < this.MaxDownloadTask && this.WaitTask > 0)
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
            int CompletedSizeCount = 0;
            do
            {
                //读取数据
                readBytesCount = responseStream.Read(bytes, 0, bytes.Length);
                //写入数据
                DownloadFileStream.Write(bytes, 0, readBytesCount);

                CompletedSizeCount += readBytesCount;//将下载数量相加
                //将完成率赋值给线程完成率
                this.Tasks[taskIndex].Threads[threadID].CompletionRate = Math.Round(((double)CompletedSizeCount / webResponse.ContentLength) * 100D, 2);//算出完成率
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
                lock(this.Tasks[taskIndex].downloadLocker)
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

    public class DownloadTask
    {
        public string Url { get; internal set; }

        public string Path { get; internal set; }

        /// <summary>
        /// 完成下载的线程数量
        /// </summary>
        public byte CompleteDownloadThreadCount { get; internal set; }

        public bool IsAlive { get; internal set; }

        public List<DownloadThread> Threads { get; internal set; }

        public string Tag { get; set; }

        public DownloadTaskComplete TaskComplete { get; set; }

        /// <summary>
        /// 下载项目的目标下载类的储存
        /// </summary>
        public Download Target { get; set; }

        public object downloadLocker = new object();

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
                    totalCompletionRate += thread.CompletionRate * (1D / this.Threads.Count);//算出每个线程的占比完成率[最终线程完成率 = 线程完成率 * (1 / 线程数)]
                }
                return totalCompletionRate;
            }
        }

        public DownloadTask(Download target)
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
                this.Threads[i].Thread = new Thread(() => this.Target.HttpDownload(taskIndex, this.Threads[index].ID));//将新建线程的赋值给属性
                //将线程设为背景线程
                this.Threads[i].Thread.IsBackground = true;
            }
        }

        public void InitializationAllThread(string tag, int eachThreadShouldDownloadSize, int remainingSize)
        {
            //将Tag赋值
            this.Tag = tag;

            //检查参数
            if(eachThreadShouldDownloadSize != 0)//判断eachThreadShouldDownloadSize是否不等于0
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
                    int[] positions = this.Target.SplitePosition(eachThreadShouldDownloadSize);
                    //将位置赋值给线程
                    this.Threads[i].DownloadPosition = positions[i];
                    //将线程的下载路径赋值给线程
                    this.Threads[i].Path = this.Target.SplitPath(this.Path, tag)[i];
                }
            }
            else
            {//否
                throw new UrlFileFileSizeIsNullOrZeroException(this.Url);//抛出错误
            }
        }

        public int StartAllThread()
        {
            //遍历所有线程数据
            foreach (DownloadThread threadData in this.Threads)
            {
                //将获取的线程数据中的线程启动
                threadData.Thread.Start();
            }
            this.RefreshPath();
            new FileStream(this.Path, FileMode.Create).Close();//创建最终文件流文件
            this.Target.TaskIndex++;
            //返回任务索引
            return this.Target.TaskIndex++;
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

        /// <summary>
        /// 刷新文件路径
        /// </summary>
        private void RefreshPath()
        {
            if (File.Exists(this.Path))
            {
                this.Path = FileHelp.AutomaticFileName(this.Path);
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
        public bool IsAlive { get; set; } = true;

        public Thread Thread { get; set; }

        /// <summary>
        /// 完成率
        /// </summary>
        public double CompletionRate { get; internal set; }

        /// <summary>
        /// 刷新文件路径
        /// </summary>
        internal void RefreshPath()
        {
            if(File.Exists(this.Path))
            {
                this.Path = FileHelp.AutomaticFileName(this.Path);
            }
        }

    }
}
