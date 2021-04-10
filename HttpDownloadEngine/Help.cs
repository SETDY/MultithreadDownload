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
            try
            {
                WebResponse testResponse = (HttpWebResponse)NetWorkHelp.CreateHttpWebRequest(url).GetResponse();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static HttpWebRequest CreateHttpWebRequest(string url)
        {
            return(HttpWebRequest)WebRequest.Create(url);
        }

        public static long GetUrlFileSize(string url)
        {
            //获得文件大小并返回
            return NetWorkHelp.CreateHttpWebRequest(url).GetResponse().ContentLength;
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
                    string temPath = path.Substring(0, path.LastIndexOf('.')) + $" ({i})" + path.Substring(path.LastIndexOf('.'));
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
}
