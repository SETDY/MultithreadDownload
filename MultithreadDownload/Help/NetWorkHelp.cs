using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Help
{
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
            return (HttpWebRequest)WebRequest.Create(url);
        }

        public static long GetUrlFileSize(string url)
        {
            //获得文件大小并返回
            return NetWorkHelp.CreateHttpWebRequest(url).GetResponse().ContentLength;
        }
    }
}
