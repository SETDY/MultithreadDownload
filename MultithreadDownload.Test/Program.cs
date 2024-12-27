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
            string url = "http://samples.mplayerhq.hu/3D/Surfcup.mp4";
            //string url = "https://sample-videos.com/video123/flv/720/big_buck_bunny_720p_1mb.flv";
            string url2 = "http://speedtest.zju.edu.cn/1000M";
            //string url = "http://updates-http.cdn-apple.com/2019WinterFCS/fullrestores/041-39257/32129B6C-292C-11E9-9E72-4511412B0A59/iPhone_4.7_12.1.4_16D57_Restore.ipsw";
            Console.WriteLine("等待回车...");
            Console.ReadLine();
            Console.WriteLine($"开始测试 链接: {url}");
            MultiDownload download = new MultiDownload(3, 16);
            Stopwatch stopwatch = Stopwatch.StartNew();
            download.Add(url, @"F:\Downloads");
            download.Add(url2, @"F:\Downloads");
            Thread.Sleep(50);
            while (download.Tasks[0].CompletionRate != 100)
            {
                Console.WriteLine($"已完成 {download.Tasks[0].CompletionRate}% {download.Tasks[0].DownloadSpeedRate}");
                Thread.Sleep(1000);
            }
            //download.Add("http://btfile.soft5566.com/y/SimAirport.Early.Access.Build.20200815.Multi.8.torrent", "F:\\");
            stopwatch.Stop();
            Console.WriteLine($"Time:{stopwatch.Elapsed.TotalSeconds}s");
            Console.WriteLine("\n完成测试");
            Console.ReadLine();
        }
    }
    //肯定有问题 φ(*￣0￣) 2021 4.8 17:00
    //我吐了（；´д｀）ゞ 什么Bug我怎么查不出来 2021:4.8 17:40
    //Stackoverflow牛逼！ヾ(≧▽≦*)o //2021.4.9 15:10
    //搞定了 q(≧▽≦q) 2021.4.9 16:02
}