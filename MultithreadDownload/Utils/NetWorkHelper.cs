using MultithreadDownload.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Help
{
    /// <summary>
    /// A class that provides network-related helper methods.
    /// </summary>
    public static class NetWorkHelper
    {
        /// <summary>
        /// A static instance of HttpClient for making HTTP requests.
        /// </summary>
        /// <remarks>
        /// Althought tt is recommended to use a single instance of HttpClient for the lifetime of the application,
        /// the class will be use a specific instance of HttpClient which is only for this class
        /// becase the HttpClient poot which now used has been use for download file.
        /// </remarks>
        private static readonly HttpClient s_client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(2000) };

        /// <summary>
        /// Checks if the given URL is valid and can be connected.
        /// </summary>
        /// <returns></returns>
        public static Result<bool> IsVaildHttpLink(string link)
        {
            // If the link is null or empty, return a failure result.
            // Otherwise, use Regex to check if the link is valid.
            if (string.IsNullOrEmpty(link))
                return Result<bool>.Failure("The link is null or empty.");
            Regex regex = new Regex("https?://");
            return Result<bool>.Success(regex.IsMatch(link));
        }

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
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, url);
                //程序运行时，如果超过2秒没有返回结果，则取消请求
                using (CancellationTokenSource cts = 
                    new CancellationTokenSource(TimeSpan.FromMilliseconds(2000)))
                {
                    var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
                    return ((int)response.StatusCode).ToString();
                }
            }
            catch (TaskCanceledException)
            {
                return "请求超时";
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

        public static async Task<long> GetUrlFileSizeAsync(string url)
        {
            try
            {
                //程序运行时，如果超过2秒没有返回结果，则取消请求
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(2000)))
                {
                    var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url), cts.Token);
                    return response.Content.Headers.ContentLength ?? 0;
                }
            }
            catch (TaskCanceledException)
            {
                return 0; // 超时返回0
            }
            catch (Exception)
            {
                return 0; // 其他异常返回0
            }
        }
    }
}
