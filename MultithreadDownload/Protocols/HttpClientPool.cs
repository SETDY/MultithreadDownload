using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Protocols
{
    /// <summary>
    /// A pool of HttpClient instances for efficient reuse.
    /// </summary>
    internal static class HttpClientPool
    {
        /// <summary>
        /// A thread-safe pool of HttpClient instances.
        /// </summary>
        private static readonly ConcurrentBag<HttpClient> ClientPool = new ConcurrentBag<HttpClient>();

        /// <summary>
        /// The maximum number of HttpClient instances in the pool.
        /// </summary>
        private const int MAX_POOL_SIZE = 6;

        /// <summary>
        /// The maximum timeout for HttpClient requests in milliseconds.
        /// </summary>
        private const uint MAX_TIME_OUT = 5000;

        static HttpClientPool()
        {
            // Pre-fill the pool with a number of HttpClient instances.
            for (int i = 0; i < MAX_POOL_SIZE / 2; i++)
            {
                ClientPool.Add(CreateHttpClient());
            }
        }

        /// <summary>
        /// Get an HttpClient instance from the pool.
        /// </summary>
        /// <returns></returns>
        public static HttpClient GetClient()
        {
            // Try to take an HttpClient instance from the pool.
            // If not available, create a new one.
            // Even if the pool is full and we can't take one, we still create a new one
            // To prevent blocking the thread and decline the efficiency of download.
            if (ClientPool.TryTake(out HttpClient client))
            {
                return client;
            }

            return CreateHttpClient();
        }

        /// <summary>
        /// Return an HttpClient instance to the pool.
        /// </summary>
        /// <param name="client"></param>
        public static void ReturnClient(HttpClient client)
        {
            // Dispose the client if the pool is full. Otherwise, add it back to the pool.
            if (ClientPool.Count < MAX_POOL_SIZE)
            {
                ClientPool.Add(client);
            }
            else
            {
                client.Dispose();
            }
        }

        /// <summary>
        /// Create a new HttpClient instance.
        /// </summary>
        /// <returns></returns>
        private static HttpClient CreateHttpClient()
        {
            return new HttpClient();
        }
    }
}
