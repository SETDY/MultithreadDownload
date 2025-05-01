using System;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

namespace MultithreadDownload.IntegrationTests.Fixtures
{
    public class LocalHttpFileServer
    {
        private readonly HttpListener s_listener = new();
        private readonly string s_baseFilePath;
        private readonly string s_url;
        private readonly bool s_noRange;

        /// <summary>
        /// The initialization of the local HTTP file server.
        /// </summary>
        /// <param name="prefixUrl">The url prefix for the HTTP server.</param>
        /// <param name="filePath">The file path to be served.</param>
        /// <remarks>
        /// The example of <paramref name="prefixUrl"/> is http://localhost:8080/ and
        /// the example of <paramref name="filePath"/> is C:\testfile.txt
        /// </remarks>
        public LocalHttpFileServer(string prefixUrl, string filePath)
        {
            s_url = prefixUrl;
            s_baseFilePath = filePath;
            s_listener.Prefixes.Add(prefixUrl);
        }

        /// <summary>
        /// The initialization of the local HTTP file server.
        /// </summary>
        /// <param name="prefixUrl">The url prefix for the HTTP server.</param>
        /// <param name="filePath">The file path to be served.</param>
        /// <param name="noRange">The flag to disable range requests.</param>
        /// <remarks>
        /// The example of <paramref name="prefixUrl"/> is http://localhost:8080/ and
        /// the example of <paramref name="filePath"/> is C:\testfile.txt
        /// </remarks>
        public LocalHttpFileServer(string prefixUrl, string filePath, bool noRange)
        {
            s_url = prefixUrl;
            s_baseFilePath = filePath;
            s_listener.Prefixes.Add(prefixUrl);
            s_noRange = noRange;
        }

        /// <summary>
        /// Start the local HTTP file server.
        /// </summary>
        public void Start()
        {
            s_listener.Start();
            Task.Run(() => HandleRequests());
        }

        /// <summary>
        /// Stop the local HTTP file server.
        /// </summary>
        public void Stop() => s_listener.Stop();

        /// <summary>
        /// Handle the requests from the HTTP server.
        /// </summary>
        /// <returns>The asynchronous operation.</returns>
        private async Task HandleRequests()
        {
            while (s_listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = await s_listener.GetContextAsync();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    if (response == null) { continue; }

                    var buffer = File.ReadAllBytes(s_baseFilePath);
                    response.AddHeader("Accept-Ranges", "bytes");

                    if(HandleRequest_HEAD(request, response, buffer)) { continue; }

                    if (await HandleRequest_Range(request, response, buffer)) { continue; }

                    HandleRequest_FullGET(request, response, buffer);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("An exception occurred while handling the request: " + ex.Message);
                }
            }
        }


        private async Task<bool> HandleRequest_Range(HttpListenerRequest request, HttpListenerResponse response
            ,byte[] buffer)
        {
            var range = request.Headers["Range"];
            // Handle range requests
            if (range != null)
            {
                var match = Regex.Match(range, @"bytes=(\d+)-(\d*)");

                if (match.Success)
                {
                    int from = int.Parse(match.Groups[1].Value);
                    int to = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : buffer.Length - 1;
                    var length = to - from + 1;
                    response.StatusCode = 206;
                    response.ContentType = "application/octet-stream";
                    response.ContentLength64 = length;
                    response.AddHeader("Content-Range", $"bytes {from}-{to}/{buffer.Length}");

                    await response.OutputStream.WriteAsync(buffer, from, length);
                    response.Close();

                    return true;
                }
            }
            return false;
        }

        private bool HandleRequest_HEAD(HttpListenerRequest request, HttpListenerResponse response
            , byte[] buffer)
        {
            // Handle HEAD request
            if (request.HttpMethod == "HEAD")
            {
                Debug.WriteLine("Processing HEAD request");
                response.ContentType = "application/octet-stream";
                response.ContentLength64 = buffer.Length;
                response.StatusCode = 200;
                response.KeepAlive = false;
                response.Close();
                return true;
            }
            return false;
        }

        private async void HandleRequest_FullGET(HttpListenerRequest request, HttpListenerResponse response
    , byte[] buffer)
        {
            // Normal full GET request
            response.ContentType = "application/octet-stream";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }
    }
}