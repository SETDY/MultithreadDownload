using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Net;
using MultithreadDownload.Logging;

namespace MultithreadDownload.IntegrationTests.Fixtures
{
    public class LocalHttpFileServer
    {
        private IHost? _host;
        private int _port;
        private readonly string _filePath;
        private readonly byte[] _fileContentBuffer;
        private readonly bool _noRange;

        public string Url => $"http://localhost:{_port}/";

        public LocalHttpFileServer(string _, string filePath, bool noRange = false)
        {
            _filePath = filePath;
            _fileContentBuffer = File.ReadAllBytes(filePath);
            _noRange = noRange;
        }

        public void Start()
        {
            if (_host != null)
                throw new InvalidOperationException("Server is already running.");
            // Pick a random free port
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            _port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            // Log the server is starting if needed
            DownloadLogger.LogInfo($"Starting LocalHttpFileServer at {Url} with file size {_fileContentBuffer.Length} bytes. No Range: {_noRange}");

            _host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel()
                        .UseUrls(Url)
                        .Configure(app =>
                        {
                            app.Use(async (context, next) =>
                            {
                                try
                                {
                                    await next();
                                }
                                catch (Exception ex)
                                {
                                    DownloadLogger.LogError("Unhandled exception during request", ex);
                                    context.Response.StatusCode = 500;
                                }
                            });

                            app.Run(async context =>
                            {
                                DownloadLogger.LogInfo($"Received request: {context.Request.Method} {context.Request.Path}");
                                var request = context.Request;
                                var response = context.Response;

                                response.Headers.Add("Accept-Ranges", "bytes");

                                if (HandleRequest_HEAD(request, response)) return;
                                if (!_noRange && await HandleRequest_Range(request, response)) return;
                                await HandleRequest_FullGET(response);
                            });
                        });
                })
                .Build();

            _host.Start();
        }

        public void Stop()
        {
            _host?.StopAsync().GetAwaiter().GetResult();
            _host?.Dispose();
        }

        private bool HandleRequest_HEAD(HttpRequest request, HttpResponse response)
        {
            if (request.Method == HttpMethods.Head)
            {
                DownloadLogger.LogInfo($"Handling HEAD request: {request.Method} {request.Path}");
                response.ContentType = "application/octet-stream";
                response.Headers.ContentLength = _fileContentBuffer.Length;
                response.StatusCode = 200;
                return true;
            }
            return false;
        }

        private async Task<bool> HandleRequest_Range(HttpRequest request, HttpResponse response)
        {
            // Log the range request details if needed
            // DownloadLogger.LogInfo($"Handling Range request: {request.Method} {request.Path} with Range header: {request.Headers["Range"]}");
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
                    // Log the successful range request processing if needed
                    //DownloadLogger.LogInfo($"Range request processed: {from}-{to} of {_fileContentBuffer.Length} bytes.");
                    return true;
                }
            }
            return false;
        }

        private async Task HandleRequest_FullGET(HttpResponse response)
        {
            response.StatusCode = 200;
            response.ContentType = "application/octet-stream";
            response.ContentLength = _fileContentBuffer.Length;
            await SafeWriteAsync(response.Body, _fileContentBuffer, 0, _fileContentBuffer.Length);
        }

        private async Task SafeWriteAsync(Stream stream, byte[] buffer, int offset, int count, int chunkSize = 81920)
        {
            // Log the start of the write operation if needed
            //DownloadLogger.LogInfo($"Writing {count} bytes to stream in chunks of {chunkSize} bytes.");
            int remaining = count;
            long totalWritten = 0;
            while (remaining > 0)
            {
                try
                {
                    int writeSize = Math.Min(chunkSize, remaining);
                    await stream.WriteAsync(buffer, offset, writeSize);
                    await stream.FlushAsync();
                    totalWritten += writeSize;
                    // Log the number of bytes written in this chunk if needed
                    //DownloadLogger.LogInfo($"Wrote {writeSize} bytes to stream. Total written: {totalWritten} bytes.");
                    offset += writeSize;
                    remaining -= writeSize;
                }
                catch (Exception ex)
                {
                    // Log the error and stop the write operation
                    DownloadLogger.LogError("An error occurred while writing to the stream. Stopping write operation.");
                    DownloadLogger.LogError(ex.Message, ex);
                }
            }
            await stream.FlushAsync();
            if (totalWritten != count)
            {
                DownloadLogger.LogError($"[Mismatch] Content-Length = {count}, but only wrote {totalWritten} bytes.");
                throw new InvalidOperationException($"Content-Length mismatch: expected {count} bytes, but wrote {totalWritten} bytes.");
            }
            // Log the successful completion of the write operation if needed
            //DownloadLogger.LogInfo("Write operation completed successfully.");
        }
    }
}
