using System;
using HttpDownloadEngine;

namespace HttpDownloadEngine.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            string url = "http://btfile.soft5566.com/y/Total.War.THREE.KINGDOMS.v1.7.0.Incl.Dlcs.Multi.13.Steam.torrent";
            Console.WriteLine("等待回车...");
            Console.ReadLine();
            Console.WriteLine($"开始测试 链接: {url}");
            Download download = new Download(3, 8);
            download.Add(url,"F:\\");
            download.Add(url,"F:\\");
            download.Add(url,"F:\\");
            download.Add("http://btfile.soft5566.com/y/SimAirport.Early.Access.Build.20200815.Multi.8.torrent", "F:\\");
            Console.WriteLine("完成测试");
            Console.ReadLine();
        }
    }
    //肯定有问题 φ(*￣0￣) 20214.8 17:00
    //我吐了（；´д｀）ゞ 什么Bug我怎么查不出来 2021:4.8 17:40
    //Stackoverflow牛逼！ヾ(≧▽≦*)o //2021.4.9 15:10
    //搞定了 q(≧▽≦q) 2021.4.9 16:02
}