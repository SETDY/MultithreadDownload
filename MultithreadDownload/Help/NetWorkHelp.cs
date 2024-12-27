using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Help
{
    public static class NetWorkHelp
    {
        private static readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        /// 是否是可连接特定网址
        /// </summary>
        /// <param name="url">网址</param>
        /// <returns></returns>
        public static async Task<bool> CanConnectionUrlAsync(string url)
        {
            if (RegexHelp.IsUrl(url) == false)//正则判断是否是Url
            {
                return false;
            }
            int internetConnectedState = 0;
            if (NetWorkHelp.InternetGetConnectedState(out internetConnectedState, 0) == false)//判断是否可以上网
            {
                return false;
            }
            if (await NetWorkHelp.GetWebStatusCodeAsync(url) != "200")//判断网站返回是否正确
            {
                return false;
            }
            return true;
        }

        //仅检测链接头，不会获取链接的结果。所以速度很快，超时的时间单位为毫秒
        public static async Task<string> GetWebStatusCodeAsync(string url)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                var response = await httpClient.SendAsync(request);
                return ((int)response.StatusCode).ToString();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        //导入判断网络是否连接的 .dll  
        [DllImport("wininet.dll", EntryPoint = "InternetGetConnectedState")]
        //判断网络状况的方法,返回值true为连接，false为未连接  
        public extern static bool InternetGetConnectedState(out int conState, int reder);

        public static HttpWebRequest CreateHttpWebRequest(string url)
        {
            return (HttpWebRequest)WebRequest.Create(url);
        }

        public static async Task<long> GetUrlFileSizeAsync(string url)
        {
            //获得文件大小并返回
            var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
            return response.Content.Headers.ContentLength ?? 0;
        }
    }
}
