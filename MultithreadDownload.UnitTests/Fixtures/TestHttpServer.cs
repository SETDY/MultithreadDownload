using System.Net;

namespace MultithreadDownload.UnitTests.Fixtures
{
    /// <summary>
    /// A simple HTTP server for testing purposes.
    /// </summary>
    public class TestHttpServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Task _serverTask;
        private readonly Func<HttpListenerRequest, HttpListenerResponse, Task> _onRequest;

        public string Url { get; }

        public TestHttpServer(string url, Func<HttpListenerRequest, HttpListenerResponse, Task> onRequest)
        {
            Url = url;
            _onRequest = onRequest;
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _listener.Start();

            // Keep the server running until cancellation is requested
            _serverTask = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    var context = await _listener.GetContextAsync();
                    await _onRequest(context.Request, context.Response);
                    context.Response.Close();
                }
            }, _cts.Token);
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
            _listener.Close();
        }
    }
}