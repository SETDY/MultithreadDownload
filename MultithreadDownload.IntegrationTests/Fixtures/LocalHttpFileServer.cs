using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

namespace MultithreadDownload.IntegrationTests.Fixtures
{
    public class LocalHttpFileServer
    {
        private readonly HttpListener _listener = new();
        private readonly string _baseFilePath;
        private readonly string _url;
        private readonly bool _noRange;

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
            _url = prefixUrl;
            _baseFilePath = filePath;
            _listener.Prefixes.Add(prefixUrl);
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
            _url = prefixUrl;
            _baseFilePath = filePath;
            _listener.Prefixes.Add(prefixUrl);
            _noRange = noRange;
        }

        /// <summary>
        /// Start the local HTTP file server.
        /// </summary>
        public void Start()
        {
            _listener.Start();
            Task.Run(() => HandleRequests());
        }

        /// <summary>
        /// Stop the local HTTP file server.
        /// </summary>
        public void Stop() => _listener.Stop();

        /// <summary>
        /// Handle the requests from the HTTP server.
        /// </summary>
        /// <returns>The asynchronous operation.</returns>
        private async Task HandleRequests()
        {
            while (_listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = await _listener.GetContextAsync();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    if (response == null) { continue; }

                    var buffer = File.ReadAllBytes(_baseFilePath);
                    response.AddHeader("Accept-Ranges", "bytes");

                    if (HandleRequest_HEAD(request, response, buffer)) { continue; }

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
            , byte[] buffer)
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
                    int length = to - from + 1;
                    // If the length is 0, it means the file is an empty file
                    // To prevent the OutOfRange exception, we set the length to 0
                    if (length == 1)
                        length = 0;
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