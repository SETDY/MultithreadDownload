using System.Net;

namespace MultithreadDownload.UnitTests
{
    /// <summary>
    /// A simple HTTP server for testing purposes.
    /// </summary>
    public class TestHttpServer : IDisposable
    {
        private readonly HttpListener s_listener;
        private readonly CancellationTokenSource s_cts = new CancellationTokenSource();
        private readonly Task s_serverTask;
        private readonly Func<HttpListenerRequest, HttpListenerResponse, Task> s_onRequest;

        public string Url { get; }

        public TestHttpServer(string url, Func<HttpListenerRequest, HttpListenerResponse, Task> onRequest)
        {
            this.Url = url;
            s_onRequest = onRequest;
            s_listener = new HttpListener();
            s_listener.Prefixes.Add(url);
            s_listener.Start();

            // Keep the server running until cancellation is requested
            s_serverTask = Task.Run(async () =>
            {
                while (!s_cts.Token.IsCancellationRequested)
                {
                    var context = await s_listener.GetContextAsync();
                    await s_onRequest(context.Request, context.Response);
                    context.Response.Close();
                }
            }, s_cts.Token);
        }

        public void Dispose()
        {
            s_cts.Cancel();
            s_listener.Stop();
            s_listener.Close();
        }
    }
}