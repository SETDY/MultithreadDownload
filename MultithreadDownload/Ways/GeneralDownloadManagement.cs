using MultithreadDownload.Downloads;
using MultithreadDownload.Help;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Ways
{
    /// <summary>
    /// 负责所有下载的共有方法（功能）的管理
    /// </summary>
    internal static class General_DownloadManagement
    {
        /// <summary>
        /// 合并文件
        /// </summary>
        /// <param name="taskIndex"></param>
        /// <param name="threadID"></param>
        internal static void Combine(DownloadTask downloadTask)
        {
            FileStream finalFileStream;//最终文件流
            FileStream tempReadFilrStream;//用于读取临时下载文件的流
            int readBytesCount;//每次读取的字节数
            //读取缓存 //读取的数据将存放在此后写入文件
            byte[] bytes = new byte[1024];//每次的读取的大小(1024最稳,4096最快)
            try
            {
                finalFileStream = new FileStream(downloadTask.Path, FileMode.Open);
            }
            catch (Exception)
            {
                return;
            }
            //遍历所有下载线程
            foreach (DownloadThread thread in downloadTask.Threads)
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

        }

        /// <summary>
        /// 分割文件
        /// </summary>
        /// <param name="remainingSize">剩余的大小(除完后的余数)</param>
        /// <returns>每个线程应该下载的大小</returns>
        internal static long SplitSize(int maxDownloadThread, string url, out long remainingSize)
        {
            long fileSize = NetWorkHelp.GetUrlFileSizeAsync(url).GetAwaiter().GetResult();

            //每一个线程应当下载的大小
            long eachThreadShouldDownloadSize = (long)fileSize / maxDownloadThread;//文件大小/最大下载线程数
            remainingSize = (long)fileSize % maxDownloadThread;//取余数
            return eachThreadShouldDownloadSize;
        }

        /// <summary>
        /// 将下载路径分割
        /// </summary>
        /// <param name="path">路径(包含文件)</param>
        /// <returns></returns>
        internal static string[] SplitPath(MultiDownload coreObject, string path, string tag)
        {
            string[] paths = new string[coreObject.MaxDownloadThread];
            for (int i = 0; i < coreObject.MaxDownloadThread; i++)
            {
                //目标文件名(不含扩展名)
                string targetFileNameWE = Path.GetFileNameWithoutExtension(path);
                //临时文件名文件扩展名
                string extension = ".Download";
                //将计算好的路径赋值
                paths[i] = Path.Combine(Path.GetDirectoryName(path), $"{targetFileNameWE} [{tag}]-{i}{extension}");
            }
            return paths;
        }


        /// <summary>
        /// 分割文件位置
        /// </summary>
        /// <param name="eachThreadShouldDownloadSize"></param>
        /// <returns>位置</returns>
        internal static long[] SplitePosition(MultiDownload coreObject, long eachThreadShouldDownloadSize)
        {
            long[] reutnPosition = new long[coreObject.MaxDownloadThread];
            for (int i = 0; i < coreObject.MaxDownloadThread; i++)
            {
                //获得文件在下载式因在哪一出位置开始下载并赋值
                reutnPosition[i] = eachThreadShouldDownloadSize * i;
            }
            return reutnPosition;
        }
    }
}
