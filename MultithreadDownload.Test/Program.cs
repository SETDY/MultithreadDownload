using System;
using System.Diagnostics;
using System.Threading;
using MultithreadDownload.Downloads;

namespace MultithreadDownload.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            //string url = "http://samples.mplayerhq.hu/3D/Surfcup.mp4";
            string url = "https://sample-videos.com/video123/flv/720/big_buck_bunny_720p_1mb.flv";
            Console.WriteLine("等待回车...");
            Console.ReadLine();
            Console.WriteLine($"开始测试 链接: {url}");
            MultiDownload download = new MultiDownload(3, 8);
            download.Add(url, @"G:\");
            stopwatch.Stop();
            Console.WriteLine($"Time:{stopwatch.Elapsed.TotalSeconds}s");
            //download.Add(url, @"F:\DownloadTest");
            //download.Add(url, @"F:\DownloadTest");
            //download.Add(url, @"F:\DownloadTest");
            Thread.Sleep(50);
            //download.Add("http://btfile.soft5566.com/y/SimAirport.Early.Access.Build.20200815.Multi.8.torrent", "F:\\");
            Console.WriteLine("\n完成测试");
            Console.ReadLine();
        }
    }
    //肯定有问题 φ(*￣0￣) 2021 4.8 17:00
    //我吐了（；´д｀）ゞ 什么Bug我怎么查不出来 2021:4.8 17:40
    //Stackoverflow牛逼！ヾ(≧▽≦*)o //2021.4.9 15:10
    //搞定了 q(≧▽≦q) 2021.4.9 16:02
}