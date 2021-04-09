using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace HttpDownloadEngine.Help
{
    public class EnumHelp
    {
        /// <summary>
        /// 获取描述信息
        /// </summary>
        public void GetDescription()
        {

        }
    }

    public static class RegexHelp
    {
        //public static bool IsUrl(string url)
        //{

        //}

        public static bool IsHttp(string url)
        {
            return Regex.IsMatch(url, "https?://");
        }
    }

    public static class NetWorkHelp
    {
        /// <summary>
        /// 是否可连接特定网址
        /// </summary>
        /// <param name="url">网址</param>
        /// <returns></returns>
        public static bool CanConnection(string url)
        {
            //获取网站返回结果并判断是否正常
            if(((HttpWebResponse)NetWorkHelp.CreateHttpWebRequest(url).GetResponse()).StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static HttpWebRequest CreateHttpWebRequest(string url)
        {
            return(HttpWebRequest)WebRequest.Create(url);
        }

        public static long GetUrlFileSize(DownloadType type,string url)
        {
            //判断是那种类型
            switch (type)
            {
                case DownloadType.HTTP:
                    //获得文件大小并返回
                    return NetWorkHelp.CreateHttpWebRequest(url).GetResponse().ContentLength;
                default:
                    throw new NullReferenceException();
            }
        }
    }


    public static class FileHelp
    {
        /// <summary>
        /// 自动获取名称相撞的文件应该取得名称
        /// </summary>
        /// <param name="path">路径 (包含文件)</param>
        public static string AutomaticFileName(string path)
        {
            //判断是否有文件的名称相撞
            if (File.Exists(path) == true)
            {
                for (int i = 1; true; i++)
                {
                    //给予临时文件名
                    string temPath = path.Substring(0, path.IndexOf('.')) + $" ({i})" + path.Substring(path.IndexOf('.'));
                    //判断临时文件名是否有文件的名称与其相撞
                    if (File.Exists(temPath) == false)
                    {
                        //返回临时文件名
                        return temPath;
                    }
                }
            }
            else
            {
                return path;
            }
        }
    }

    public static class DownloadAddressHelp
    {
        //    public static DownloadType GetAddressType(string address)
        //    {
        //        if(RegexHelp.IsHttp(address))
        //        {

        //        }
        //    }
    }
}
