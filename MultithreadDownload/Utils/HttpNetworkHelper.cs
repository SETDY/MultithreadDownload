using System;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Primitives
{
    /// <summary>
    /// A class that provides http network-related helper methods.
    /// </summary>
    public static class HttpNetworkHelper
    {
        /// <summary>
        /// A static instance of HttpClient for making HTTP requests.
        /// </summary>
        /// <remarks>
        /// 1. Althought it is recommended to use a single instance of HttpClient for the lifetime of the application,
        /// the class will be use a specific instance of HttpClient which is only for this class
        /// becase the <see cref="Protocols.Http.HttpClientPool"/> which now used has been use for download file.
        /// 2. Since the timeout of <see cref="_client"/> has been controlled by cts in the methods,
        /// the default timeout of <see cref="_client"/> is set to infinite.
        /// </remarks>
        private static readonly HttpClient _client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

        #region Winapi

        /// <summary>
        /// Get the current network connection state by using the WinInet API.
        /// </summary>
        /// <param name="conState">Internet Connected State</param>
        /// <param name="reder">should be 0</param>
        /// <returns>Whether the Internet is connected</returns>
        /// <remarks>
        /// Since the method is from WinInet API, it is only available on Windows.
        /// Therefore, if other method is used this method, it will be checked if the OS is Windows and
        /// bypass this method if it is not Windows.
        /// </remarks>
        [DllImport("wininet.dll", EntryPoint = "InternetGetConnectedState")]
        private static extern bool InternetGetConnectedState(out int conState, int reder);

        #endregion Winapi

        /// <summary>
        /// Checks if the system is connected to the Internet.
        /// </summary>
        /// <returns>Is the system connected to the Internet</returns>
        /// <remarks>
        /// If the OS is Windows, use the WinInet API to check the connection state.
        /// Otherwise, use HttpClient to check if the system is connected to the Internet.
        /// </remarks>
        private static bool IsConnectedInternet()
        {
            // Check if the OS is Windows and call the WinInet API to get the connection state.
            // If the OS is not Windows, do a complcated check by using HttpClient.
            // Because of the complexity of the check, it should not be used to use in Windows.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return InternetGetConnectedState(out _, 0);
            }

            // If the OS is not Windows, use HttpClient to check if the system is connected to the Internet.
            // If the request takes longer than 10 seconds, cancel it.
            // If the request fails, return false.
            // Note:
            // Since http://www.baidu.com is a well-known website and can be connected all over the world ,
            // it is used to check if the system is connected to the Internet.
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://www.baidu.com");
            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                try
                {
                    HttpResponseMessage response = _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token).Result;
                    return response.IsSuccessStatusCode;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Checks if the given URL is valid and can be connected.
        /// </summary>
        /// <returns>Whether the http link can be connected</returns>
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
        /// Get the status code of a web page asynchronously.
        /// </summary>
        /// <remarks>
        /// Since the method only check the status code of the web page,
        /// it is fast and does not require downloading the entire page.
        /// </remarks>
        /// <param name="link"></param>
        /// <returns></returns>
        public static async Task<HttpStatusCode> GetWebStatusCodeAsync(string link)
        {
            try
            {
                // Send a HEAD request to the URL to get the status code.
                // If the request takes longer than 10 seconds, cancel it.
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, link);
                using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    HttpResponseMessage response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
                    return response.StatusCode;
                }
            }
            catch (TimeoutException)
            {
                return HttpStatusCode.InternalServerError;
            }
        }

        /// <summary>
        /// Checks if the given URL can be connected.
        /// </summary>
        /// <param name="link">The http link you want to check<param>
        /// <returns>Whether the link can be connected</returns>
        public static async Task<bool> LinkCanConnectionAsync(string link)
        {
            Result<bool> primaryResult = IsVaildHttpLink(link);
            // Note: There should be use primaryResult.Value instead of primaryResult.IsSuccess
            //       because the IsSuccess only check if IsVaildHttpLink() is success,
            //       but not check if the link is valid.
            if (!primaryResult.Value) { return false; }
            if (!IsConnectedInternet()) { return false; }
            if (await GetWebStatusCodeAsync(link) != HttpStatusCode.OK) { return false; }
            return true;
        }

        /// <summary>
        /// Get the file size of a link asynchronously.
        /// </summary>
        /// <param name="link">The link you want to get the file size</param>
        /// <returns>The file size of the link as bytes</returns>
        public static async Task<Result<long>> GetLinkFileSizeAsync(string link)
        {
            // If the link is null or empty, return 0.
            // Otherwise, send a HEAD request to the URL to get the file size and return its file size.
            // If the request takes longer than 2 seconds, cancel it.
            // If the request fails, return Failure.
            if (string.IsNullOrEmpty(link)) { return Result<long>.Failure($"{link} cannot be null or emprt."); }
            try
            {
                using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
                {
                    long fileSize = (await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, link), cts.Token))
                        .Content.Headers.ContentLength ?? 0;

                    if (fileSize < 0) { return Result<long>.Failure("Failed to get the file size."); }
                    return Result<long>.Success(fileSize);
                }
            }
            catch (Exception)
            {
                return Result<long>.Failure("Failed to get the file size.");
            }
        }
    }
}