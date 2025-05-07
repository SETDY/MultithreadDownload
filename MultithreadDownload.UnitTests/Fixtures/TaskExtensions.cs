using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.UnitTests.Fixtures
{
    /// <summary>
    /// Extension methods for Task to handle timeout scenarios.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Waits for a task to complete within a specified timeout period.
        /// </summary>
        /// <typeparam name="T">The type of the result produced by the task.</typeparam>
        /// <param name="task">The task to wait for.</param>
        /// <param name="timeout">The timeout period.</param>
        /// <returns>The result of the task if it completes within the timeout.</returns>
        /// <exception cref="TimeoutException">The operation has timed out.</exception>
        public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    return await task; // Task completed within timeout
                }
                else
                {
                    throw new TimeoutException("The operation has timed out.");
                }
            }
        }
    }
}
