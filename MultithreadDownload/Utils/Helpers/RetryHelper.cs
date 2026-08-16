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
        private static Random RandomInstance { get; } = new Random();

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
        public static async Task<Result<T, E>> RetryAsync<T, E>(
            uint maxRetries,
            TimeSpan baseDelay,
            TimeSpan maxDelay,
            Func<Task<Result<T, E>>> operation,
            CancellationToken cancellationToken)
        {
            Result<T, E> result = null;

            for (uint i = 0; i < maxRetries; i++)
            {
                // Check if the cancellation is requested before each attempt, and throw an OperationCanceledException if it is.
                cancellationToken.ThrowIfCancellationRequested();

                // Excute the operation and check if it is successful
                // If the operation is successful, return the result immediately
                // Otherwise, wait for a certain amount of time before retrying the operation again
                result = await operation();
                if (result.IsSuccess)
                {
                    return result;
                }
                Thread.Sleep(GetDelay(baseDelay, maxDelay, i));
            }
            // If all attempts fail, return the result of onFinalFailure
            return Result<T, E>.Failure(result.ErrorState);
        }

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
        public static async Task<Result<T, E>> RetryAsync<T, E>(
            uint maxRetries,
            TimeSpan baseDelay,
            TimeSpan maxDelay,
            Func<Task<Result<T, E>>> operation,
            Func<E> onFinalFailure,
            CancellationToken cancellationToken)
        {
            for (uint i = 0; i < maxRetries; i++)
            {
                // Check if the cancellation is requested before each attempt, and throw an OperationCanceledException if it is.
                cancellationToken.ThrowIfCancellationRequested();

                // Excute the operation and check if it is successful
                // If the operation is successful, return the result immediately
                // Otherwise, wait for a certain amount of time before retrying the operation again
                Result<T, E> result = await operation();
                if (result.IsSuccess)
                {
                    return result;
                }
                Thread.Sleep(GetDelay(baseDelay, maxDelay, i));
            }
            // If all attempts fail, return the result of onFinalFailure
            return Result<T, E>.Failure(onFinalFailure());
        }


        private static TimeSpan GetDelay(TimeSpan baseDelay, TimeSpan maxDelay, uint attempt)
        {
            // Calculate the delay using exponential backoff
            double exponential = baseDelay.TotalMilliseconds * Math.Pow(2, attempt);
            // Calculate the capped delay to prevent it from growing too large
            double capped = Math.Min(exponential, maxDelay.TotalMilliseconds);

            // Calculate the jitter
            int jitter = RandomInstance.Next(0, (int)capped) / 2;

            // Calculate the final delay and return it as a TimeSpan
            return TimeSpan.FromMilliseconds(capped / 2 + jitter);
        }
    }

}
