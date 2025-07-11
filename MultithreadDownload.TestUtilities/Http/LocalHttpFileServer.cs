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
using MultithreadDownload.Primitives;

namespace MultithreadDownload.IntegrationTests.Fixtures
{
    /// <summary>
    /// LocalHttpFileServer is a simple HTTP server that serves files from a local byte array.
    /// </summary>
    public class LocalHttpFileServer
    {
        #region Fields and Properties
        /// <summary>
        /// Represents the HTTP server instance used to serve files locally.
        /// </summary>
        private IHost? _host;

        /// <summary>
        /// The port on which the HTTP server is running.
        /// </summary>
        private int _port = -1;

        /// <summary>
        /// The buffer containing the file content to be served by the HTTP server.
        /// </summary>
        private readonly byte[] _testDataBuffer;

        /// <summary>
        /// Indicates whether the server should handle range requests.
        /// </summary>
        private readonly bool _noRange;

        /// <summary>
        /// Gets the URL of the local HTTP server.
        /// </summary>
        public string Url
        {
            get
            {
                // If the local server has not been started, _port is -1, and we have to throw an exception.
                if (_port == -1)
                {
                    Exception exception = new InvalidOperationException("The local HTTP server has not been started yet. Please call Start() before accessing the URL.");
                    DownloadLogger.LogError("Cannot attempte to access URL before server start", exception);
                }
                // Otherwise, return the URL with the assigned port.
                return $"http://localhost:{_port}/";
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the LocalHttpFileServer with the specified file path.
        /// </summary>
        /// <param name="filePath">The real path to the file to be served.</param>
        /// <param name="noRange">Whether to disable range requests.</param>
        public LocalHttpFileServer(string filePath, bool noRange = false)
            : this(File.ReadAllBytes(filePath), noRange)
        { }

        /// <summary>
        /// Initializes a new instance of the LocalHttpFileServer with the specified test data.
        /// </summary>
        /// <param name="testData">The byte array containing the test data to be served.</param>
        /// <param name="noRange">Whether to disable range requests.</param>
        public LocalHttpFileServer(byte[] testData, bool noRange = false)
        {
            _testDataBuffer = testData;
            _noRange = noRange;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Creates and hosts the local HTTP file server, assigning an available port and preparing it to serve files.
        /// </summary>
        /// <exception cref="InvalidOperationException">The exception is thrown if the server is already created.</exception>
        public void Create()
        {
            // Ensure the server is not already running
            if (_host != null)
                throw new InvalidOperationException("Server is already created.");
            // Get an available port for the server to listen on
            _port = GetAvailablePort();
            // Log the server creation if needed
            DownloadLogger.LogInfo($"LocalHttpFileServer created at {Url} with file size {_testDataBuffer.Length} bytes. No Range: {_noRange}");

            // Get an available port for the server to listen on
            _port = GetAvailablePort();
            // Log the server is starting if needed
            DownloadLogger.LogInfo($"Starting LocalHttpFileServer at {Url} with file size {_testDataBuffer.Length} bytes. No Range: {_noRange}");
            // Create and configure the host for the HTTP server
            _host = Host
                .CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseKestrel()
                        .UseUrls(Url)
                        .Configure(app =>
                        {
                            // Use a middleware to handle global exceptions
                            app.Use(HandleGlobalExceptionsAsync);
                            // Use a middleware to handle incoming requests
                            app.Run(async context => await HandleRequestAsync(context));
                        });
                })
                .Build();
        }

        /// <summary>
        /// Starts the local HTTP file server, allowing it to accept incoming requests.
        /// </summary>
        /// <exception cref="InvalidOperationException">The exception is thrown if the server is not created before starting.</exception>
        public void Start()
        {
            // Ensure the server is not already running
            if (_host == null)
                throw new InvalidOperationException("Server is not created. Please call Create() before starting the server.");

            // Start the HTTP server asynchronously
            _host.Start();
        }

        /// <summary>
        /// Stops the local HTTP file server, releasing any resources it holds.
        /// </summary>
        public void Stop()
        {
            _host?.StopAsync().GetAwaiter().GetResult();
            _host?.Dispose();
        }

        #endregion

        #region Private Methods for the method Start()

        /// <summary>
        /// Gets an available port for the HTTP server to listen on.
        /// </summary>
        /// <returns>The available port number.</returns>
        private int GetAvailablePort()
        {
            // Create a TcpListener to find an available port
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            // Start the listener to bind to an available port
            listener.Start();
            // Get the port number assigned by the system
            int availablePort = ((IPEndPoint)listener.LocalEndpoint).Port;
            // Stop the listener as we only needed it to find an available port
            listener.Stop();
            return availablePort;
        }

        /// <summary>
        /// Handles global exceptions during request processing.
        /// </summary>
        /// <param name="context">The HTTP context for the request.</param>
        /// <param name="next">The next middleware in the pipeline.</param>
        private async Task HandleGlobalExceptionsAsync(HttpContext context, Func<Task> next)
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
        }

        /// <summary>
        /// Handles incoming HTTP requests by routing them to the appropriate method based on the request type.
        /// </summary>
        /// <param name="context">The Http context containing the HTTP request and response.</param>
        /// <returns>The task representing the asynchronous operation.</returns>
        private async Task HandleRequestAsync(HttpContext context)
        {
            // Log the request details
            DownloadLogger.LogInfo($"Local Http Server has received a request: {context.Request.Method} {context.Request.Path}");
            // Unpack the request and response objects
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;
            // Set the response headers
            response.Headers.Add("Accept-Ranges", "bytes");

            // Handle each of the request types
            // If the request is a HEAD request, handle it and return
            if (TryHandleRequest_HEAD(request, response)) return;
            // If the request is a Range request, handle it and return
            if (!_noRange && await TryHandleRequest_Range(request, response)) return;
            // Otherwise, handle a full GET request
            await HandleRequest_FullGET(response);
        }
        #endregion

        #region Handler Methods

        /// <summary>
        /// Handles a HEAD request by setting the response headers without sending the body.
        /// </summary>
        /// <param name="request">The HTTP request to handle.</param>
        /// <param name="response">The HTTP response to write the headers to.</param>
        /// <returns>Whether the request has been handled.</returns>
        private bool TryHandleRequest_HEAD(HttpRequest request, HttpResponse response)
        {
            // If the request method is not HEAD,
            // return false to indicate that this handler does not process the request
            if (!HttpMethods.IsHead(request.Method))
                return false;
            // Log the HEAD request details if needed
            DownloadLogger.LogInfo($"Handling HEAD request: {request.Method} {request.Path}");
            // Set the response headers for HEAD request
            response.ContentType = "application/octet-stream";
            response.Headers.ContentLength = _testDataBuffer.Length;
            response.StatusCode = 200;
            // Return true to indicate that the request has been handled
            return true;
        }

        /// <summary>
        /// Handles a Range request by parsing the Range header and writing the requested byte range to the response body.
        /// </summary>
        /// <param name="request">The HTTP request to handle.</param>
        /// <param name="response">The HTTP response to write the requested range to.</param>
        /// <returns>Whether the request has been handled successfully.</returns>
        /// <exception cref="InvalidOperationException">The exception is thrown if the requested range length is outside the valid range.</exception>
        private async Task<bool> TryHandleRequest_Range(HttpRequest request, HttpResponse response)
        {
            // Log the range request details if needed
            // DownloadLogger.LogInfo($"Handling Range request: {request.Method} {request.Path} with Range header: {request.Headers["Range"]}");
            // Unpack the Range header from the request
            string range = request.Headers["Range"].ToString();
            // If the Range header is null or empty, return false to indicate that this handler does not process the request
            if (!string.IsNullOrEmpty(range))
                return false;
            // Otherwise, try to parse the Range header to get the byte range
            (long from, long to) requestedRange = ParseRangeHeader(range);
            // If the parsed range is invalid, handle the failed range request
            if (requestedRange.from > requestedRange.to || 
                requestedRange.from < 0 || 
                requestedRange.from >= _testDataBuffer.Length)
                return HandleFailedRangeRequest(response, requestedRange);
            // Otherwise, handle the successful range request
            // Set the response headers for a successful range request and get the length of the requested range
            long length = SetRangeRequest_ResponseHeaders(response, requestedRange);
            // Since the stream length is int but the HttpResponse.ContentLength is a long,
            // we need to check if the length is within the valid range
            // If the length is outside the valid range, the response cannot be handled correctly
            // and the error is too difficult to handle correctly by HandleFailedRangeRequest.
            // Therefore, we throw an exception to indicate the error
            if (length > int.MaxValue || length < int.MinValue)
                throw new InvalidOperationException($"Invalid range length: {length}. It must be between {int.MinValue} and {int.MaxValue} bytes.");
            // Otherwise, proceed to write the requested range to the response body
            // Write the requested range to the response body
            await SafeWriteAsync(response.Body, _testDataBuffer, Convert.ToInt32(requestedRange.from), Convert.ToInt32(length));
            // Log the successful range request processing if needed
            //DownloadLogger.LogInfo($"Range request processed: {from}-{to} of {_fileContentBuffer.Length} bytes.");
            // Return true to indicate that the request has been handled successfully
            return true;
        }

        #region The private methods for the method TryHandleRequest_Range()

        /// <summary>
        /// Parses the Range header from the request and returns the byte range as a tuple.
        /// </summary>
        /// <param name="rangeHeader">The Range header string from the request.</param>
        /// <returns>The byte range as a tuple containing the start and end positions.</returns>
        private (long from, long to) ParseRangeHeader(string rangeHeader)
        {
            // Use a regular expression to match the range format
            Match match = Regex.Match(rangeHeader, @"bytes=(\d+)-(\d*)");
            // If the match is not successful, it indicates that the range header is not in the expected format.
            // In this case, return (-1, -1) to indicate an invalid range.
            if (!match.Success)
                return (-1, -1);
            // Parse the range values from the matched groups
            // Convert the first group to a long value for the start of the range
            long from = Convert.ToInt64(match.Groups[1].Value);
            // If the second group is not present, that means the range goes to the end of the file.
            // Therefore, set the end of the range to the last byte of the file content buffer.
            long to = match.Groups[2].Success ? Convert.ToInt64(match.Groups[2].Value) : _testDataBuffer.Length - 1;
            return (from, to);
        }

        /// <summary>
        /// Handles a failed range request by setting the appropriate response status code and headers.
        /// </summary>
        /// <remarks>
        /// If the requested range is invalid, this method sets the response status code to 416 (Requested Range Not Satisfiable)
        /// </remarks>
        /// <param name="response">The HTTP response to modify.</param>
        /// <param name="requestedRange">The requested byte range as a tuple.</param>
        /// <returns>Whether the request has been handled.</returns>
        private bool HandleFailedRangeRequest(HttpResponse response, (long from, long to)? requestedRange = null)
        {
            // If the range is invalid, set the response status code to 416 (Requested Range Not Satisfiable)
            response.StatusCode = 416;
            // Set the Content-Range header to indicate the total size of the file
            response.Headers["Content-Range"] = $"bytes */{_testDataBuffer.Length}";
            // Log the error if needed
            if (requestedRange.HasValue)
                DownloadLogger.LogError($"Invalid range request: {requestedRange.Value.from}-{requestedRange.Value.to} for file size {_testDataBuffer.Length} bytes.");
            else
                DownloadLogger.LogError($"Invalid range request with no specific range provided for file size {_testDataBuffer.Length} bytes.");
            return true; // Indicate that the request has been handled
        }

        /// <summary>
        /// Sets the response headers for a successful range request and returns the length of the requested range.
        /// </summary>
        /// <param name="response">The HTTP response to modify.</param>
        /// <param name="requestedRange">The requested byte range as a tuple.</param>
        /// <returns>The content length of the requested range, or -1 if the range is invalid.</returns>
        private long SetRangeRequest_ResponseHeaders(HttpResponse response, (long from, long to) requestedRange)
        {
            // Caculate the length of the requested range
            long length = requestedRange.to - requestedRange.from + 1;
            if (length < 0)
                return -1;
            // Set the response headers for a successful range request
            // Set the response status code to 206 (Partial Content)
            response.StatusCode = 206;
            // Set the Content-Type header to indicate the content type of the response
            response.ContentType = "application/octet-stream";
            // Set the Content-Length header to the length of the requested range
            response.ContentLength = length;
            // Set the Content-Range header to indicate the range of bytes being returned
            response.Headers["Content-Range"] = $"bytes {requestedRange.from}-{requestedRange.to}/{_testDataBuffer.Length}";
            // Log the successful range request processing if needed
            return length;
        }

        /// <summary>
        /// Writes the specified buffer to the provided stream in chunks, ensuring that the entire buffer is written.
        /// </summary>
        /// <param name="stream">The stream of the HTTP response to write to.</param>
        /// <param name="buffer">The test data buffer containing the file content.</param>
        /// <param name="offset">The offset in the buffer from which to start writing.</param>
        /// <param name="count">The number of bytes to write from the buffer starting at the offset.</param>
        /// <param name="chunkSize">The size of each chunk to write to the stream. Default is 81920 bytes.</param>
        /// <returns>The task representing the asynchronous write operation.</returns>
        /// <exception cref="InvalidOperationException">The exception is thrown if the total number of bytes written does not match the expected count.</exception>
        private async Task SafeWriteAsync(Stream stream, byte[] buffer, int offset, int count, int chunkSize = 81920)
        {
            // Initialize the remaining bytes to write and the total number of bytes written
            int remaining = count;
            int totalWritten = 0;
            while (remaining > 0)
            {
                try
                {
                    // Calculate the size of the next chunk to write, by using Math.Min to ensure we do not exceed the remaining bytes
                    int writeSize = Math.Min(chunkSize, remaining);
                    // Write the chunk to the stream asynchronously
                    await stream.WriteAsync(buffer, offset, writeSize);
                    // Flush the stream to ensure the data is sent immediately
                    await stream.FlushAsync();
                    // Update the total number of bytes written and the remaining bytes to write
                    totalWritten += writeSize;
                    offset += writeSize;
                    remaining -= writeSize;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ERROR: " + ex.Message);
                    // Log the error and stop the write operation
                    DownloadLogger.LogError("An error occurred while writing to the stream. Stopping write operation.", ex);
                    // Dispose the stream if there is an error
                    stream.Dispose();
                }
            }
            if (totalWritten != count)
                throw new InvalidOperationException($"Content-Length mismatch: expected {count} bytes, but wrote {totalWritten} bytes.");
        }
        #endregion

        /// <summary>
        /// Handles a full GET request by writing the entire file content to the response body.
        /// </summary>
        /// <param name="response">The HTTP response to write the file content to.</param>
        /// <returns>The task representing the asynchronous write operation.</returns>
        private async Task HandleRequest_FullGET(HttpResponse response)
        {
            response.StatusCode = 200;
            response.ContentType = "application/octet-stream";
            response.ContentLength = _testDataBuffer.Length;
            await SafeWriteAsync(response.Body, _testDataBuffer, 0, _testDataBuffer.Length);
        }
        #endregion
    }
}
