using MultithreadDownload.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Utils
{
    /// <summary>
    /// Provides helper methods for retrying operations with a specified number of attempts and wait time between attempts.
    /// </summary>
    public static class RetryHelper
    {
        /// <summary>
        /// Retries the specified operation up to a maximum number of attempts, waiting for a specified duration between attempts.
        /// </summary>
        /// <typeparam name="T">The type of the result value.</typeparam>
        /// <typeparam name="E">The type of the error value.</typeparam>
        /// <param name="maxRetries">The maximum number of retry attempts.</param>
        /// <param name="waitMilliseconds">The number of milliseconds to wait between attempts.</param>
        /// <param name="operation">The operation to be retried, represented as a function that returns a <see cref="Result{T, E}"/>.</param>
        /// <param name="onFinalFailure">The function to be called to get the final failure value if all attempts fail.</param>
        /// <returns>When the operation succeeds, returns the successful result; otherwise, returns the result of <paramref name="onFinalFailure"/>.</returns>
        public static Result<T, E> Retry<T, E>(
            int maxRetries,
            int waitMilliseconds,
            Func<Result<T, E>> operation,
            Func<E> onFinalFailure)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                var result = operation();
                if (result.IsSuccess)
                    return result;

                Thread.Sleep(waitMilliseconds);
            }

            return Result<T, E>.Failure(onFinalFailure());
        }
    }

}
