using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Net.Http;

namespace MultithreadDownload.IntegrationTests.Fixtures
{
    public class LocalHttpFileServer : IDisposable
    {
        private readonly byte[] _fileContentBuffer;
        private readonly bool _noRange;
        private readonly TestServer _server;
        private readonly IHost _host;
        private readonly string _url = "http://localhost:5001/";

        public string Url => _url;

        public LocalHttpFileServer(string prefixUrl, string filePath)
            : this(prefixUrl, filePath, false) { }

        public LocalHttpFileServer(string prefixUrl, string filePath, bool noRange)
        {
            _url = prefixUrl.EndsWith("/") ? prefixUrl : prefixUrl + "/";
            _fileContentBuffer = File.ReadAllBytes(filePath);
            _noRange = noRange;

            _host = new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer();
                    webBuilder.Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            var request = context.Request;
                            var response = context.Response;
                            response.Headers.Add("Accept-Ranges", "bytes");

                            if (HandleRequest_HEAD(request, response)) return;

                            if (!_noRange && await HandleRequest_Range(request, response)) return;

                            await HandleRequest_FullGET(response);
                        });
                    });
                })
                .Start();

            _server = _host.GetTestServer();
        }

        public void Start() { /* no-op: TestServer is started in constructor */ }

        public void Stop() => Dispose();

        public HttpClient CreateClient() => _server.CreateClient();

        private bool HandleRequest_HEAD(HttpRequest request, HttpResponse response)
        {
            if (request.Method == HttpMethods.Head)
            {
                Debug.WriteLine("Processing HEAD request");
                response.ContentType = "application/octet-stream";
                response.ContentLength = _fileContentBuffer.Length;
                response.StatusCode = 200;
                return true;
            }
            return false;
        }

        private async Task<bool> HandleRequest_Range(HttpRequest request, HttpResponse response)
        {
            var range = request.Headers["Range"].ToString();
            if (!string.IsNullOrEmpty(range))
            {
                var match = Regex.Match(range, @"bytes=(\d+)-(\d*)");
                if (match.Success)
                {
                    int from = int.Parse(match.Groups[1].Value);
                    int to = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : _fileContentBuffer.Length - 1;

                    if (from > to || from < 0 || from >= _fileContentBuffer.Length)
                    {
                        response.StatusCode = 416;
                        response.Headers["Content-Range"] = $"bytes */{_fileContentBuffer.Length}";
                        return true;
                    }

                    to = Math.Min(to, _fileContentBuffer.Length - 1);
                    int length = to - from + 1;

                    response.StatusCode = 206;
                    response.ContentType = "application/octet-stream";
                    response.ContentLength = length;
                    response.Headers["Content-Range"] = $"bytes {from}-{to}/{_fileContentBuffer.Length}";

                    await SafeWriteAsync(response.Body, _fileContentBuffer, from, length);
                    return true;
                }
            }
            return false;
        }

        private async Task HandleRequest_FullGET(HttpResponse response)
        {
            response.ContentType = "application/octet-stream";
            response.ContentLength = _fileContentBuffer.Length;
            await response.Body.WriteAsync(_fileContentBuffer, 0, _fileContentBuffer.Length);
        }

        private async Task SafeWriteAsync(Stream stream, byte[] buffer, int offset, int count, int chunkSize = 81920)
        {
            int remaining = count;
            while (remaining > 0)
            {
                int writeSize = Math.Min(chunkSize, remaining);
                await stream.WriteAsync(buffer, offset, writeSize);
                offset += writeSize;
                remaining -= writeSize;
            }
        }

        public void Dispose()
        {
            _host.Dispose();
        }
    }
}
