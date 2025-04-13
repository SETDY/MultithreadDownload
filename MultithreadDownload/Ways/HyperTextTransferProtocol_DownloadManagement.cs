using MultithreadDownload.Downloads;
using MultithreadDownload.Exceptions;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace MultithreadDownload.Ways
{
    internal class HyperTextTransferProtocol_DownloadManagement
    {
        private MultiDownload MultiDownload { get; }

        /// <summary>
        /// 重试次数
        /// </summary>
        private const uint TRYTIMES = 5;

        /// <summary>
        /// 重试等待时间
        /// </summary>
        private const uint WAITTIMES = 5000;

        /// <summary>
        /// 最大超时时间 (毫秒)
        /// </summary>
        private const uint MAXTIMEOUT = 5000;

        public HyperTextTransferProtocol_DownloadManagement(MultiDownload multiDownload)
        {
            MultiDownload = multiDownload;
        }

        internal Stream GetStream(DownloadTaskThreadInfo taskThreadInfo)
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
                //程序运行时，如果超过MAXTIMEOUT毫秒没有返回结果，则取消请求
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(MAXTIMEOUT)))
                {
                    //发送请求并获得Http回应
                    HttpResponseMessage response = httpClient.Send(httpRequestMessage);
                    if (response == null)
                    {
                        Debug.WriteLine($"线程{taskThreadInfo.ThreadID} 请求失败");
                        throw new UrlCanNotConnectionException();
                    }

                    Debug.WriteLine($"线程{taskThreadInfo.ThreadID} 请求成功");
                    //检查请求是否成功
                    response.EnsureSuccessStatusCode();
                    //赋值响应流
                    Stream responseStream = response.Content.ReadAsStream();
                    return responseStream;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal int DownloadFile(Stream responseStream, FileStream downloadFileStream, DownloadTaskThreadInfo taskThreadInfo)
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
                        MultiDownload.Tasks[taskThreadInfo.TaskID].Cancel();//取消该任务
                        break;
                    }
                    //读取数据
                    readBytesCount = responseStream.Read(bytes, 0, bytes.Length);
                }
                catch (Exception)
                {
                    Debug.WriteLine($"线程[{taskThreadInfo.ThreadID}] 读取数据失败，等待{WAITTIMES / 1000}秒后进行第{tryCount + 1}次重试");
                    tryCount++;
                    Thread.Sleep((int)WAITTIMES);//等待WAITTIMES
                }
            }
            while (tryCount != 0);

            //下载状态是否为取消
            if (MultiDownload.Tasks[taskThreadInfo.TaskID].State == DownloadTaskState.Cancelled)// 判断状态是否为取消
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

        internal void CaculateCompleteNumber(DownloadTaskThreadInfo taskThreadInfo, int readBytesCount)
        {
            MultiDownload.Tasks[taskThreadInfo.TaskID].Threads[taskThreadInfo.ThreadID].CompletedSizeCount += readBytesCount;//将下载数量相加
                                                                                                                             //将完成率赋值给线程完成率
            MultiDownload.Tasks[taskThreadInfo.TaskID].Threads[taskThreadInfo.ThreadID].CompletionRate =
                (float)Math.Round(((float)MultiDownload.Tasks[taskThreadInfo.TaskID].Threads[taskThreadInfo.ThreadID].CompletedSizeCount / MultiDownload.Tasks[taskThreadInfo.TaskID].
                Threads[taskThreadInfo.ThreadID].EachThreadShouldDownloadSize) * 100F, 2);//算出完成率
        }

        internal void PostDownload(FileStream downloadFileStream, DownloadTaskThreadInfo taskThreadInfo)
        {
            if (MultiDownload.Tasks[taskThreadInfo.TaskID].Threads[taskThreadInfo.ThreadID].IsAlive == true)//判断是否线程是否被设为"工作"
            {//是
                downloadFileStream.Flush();//释放所有数据
                downloadFileStream.Close();//解除对文件的占用
                lock (MultiDownload.Tasks[taskThreadInfo.TaskID].DownloadLocker)//保证每次只有一个线程操作
                {
                    MultiDownload.Tasks[taskThreadInfo.TaskID].CompleteDownloadThreadCount++;//完成下载的线程数量增加
                }

                if (MultiDownload.Tasks[taskThreadInfo.TaskID].CompleteDownloadThreadCount
                    == MultiDownload.MaxDownloadThread && new FileInfo((MultiDownload.Tasks[taskThreadInfo.TaskID].Path)).Length == 0)//是否所有线程完成下载
                {
                    //开始合并文件
                    General_DownloadManagement.Combine(taskThreadInfo.MultiDownloadObject.Tasks[taskThreadInfo.TaskID]);
                }
                DownloadTask.EndTask(taskThreadInfo.MultiDownloadObject.Tasks[taskThreadInfo.TaskID]);
            }
            else
            {//否
                downloadFileStream.Flush();//释放所有数据
                downloadFileStream.Close();//解除对文件的占用
                File.Delete(taskThreadInfo.Path);//删除下载的文件
                return;//返回
            }
        }
    }
}