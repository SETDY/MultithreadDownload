using System;
using System.Collections.Generic;

namespace MultithreadDownload.Primitives
{
    /// <summary>
    /// Represents the result of an operation, which can be either a success or a failure.
    /// Similar to the Result type in Rust.
    /// </summary>
    /// <typeparam name="T">The type of the value returned when the operation is successful.</typeparam>
    public class Result<T, ErrorCode>
    {
        #region Properties
        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets the value of the result if the operation was successful; otherwise, null.
        /// </summary>
        public Option<T> Value { get; }

        /// <summary>
        /// Gets the error state if the operation failed; otherwise, null.
        /// </summary>
        public ErrorCode ErrorState { get; }
        #endregion

        #region Constructors
        private Result(T value)
        {
            IsSuccess = true;
            Value = Option<T>.Some(value);
            ErrorState = default;
        }

        private Result(ErrorCode errorCode)
        {
            IsSuccess = false;
            ErrorState = errorCode;
            Value = default;
        }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <param name="value">The value to return if the operation is successful.</param>
        /// <returns>A <see cref="Result{T}"/> instance indicating success.</returns>
        public static Result<T, ErrorCode> Success(T value) => new Result<T, ErrorCode>(value);

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        /// <param name="errorMessage">The error message describing why the operation failed.</param>
        /// <returns>A <see cref="Result{T}"/> instance indicating failure.</returns>
        public static Result<T, ErrorCode> Failure(ErrorCode errorCode) => new Result<T, ErrorCode>(errorCode);
        #endregion

        #region Fluent Methods
#nullable enable
        /// <summary>
        /// Unwraps the result value or throws an exception if the operation failed.
        /// </summary>
        /// <param name="message">Optional custom error message to throw.</param>
        /// <returns>The value if successful.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the result is a failure.</exception>
        public T UnwrapOrThrow(string? message = null)
        {
            if (IsSuccess)
                return Value.Value!;
            throw new InvalidOperationException(message ?? $"Result was failure: {ErrorState}");
        }
#nullable disable

        /// <summary>
        /// Unwraps the result value or returns a fallback value if the operation failed.
        /// </summary>
        /// <param name="fallback">The fallback value to return if the operation failed.</param>
        /// <returns>The value if successful, otherwise the fallback value.</returns>
        public T UnwrapOr(T fallback)
        {
            return IsSuccess ? Value.Value : fallback;
        }

        /// <summary>
        /// Maps the success value to a new type using the provided selector function.
        /// </summary>
        /// <typeparam name="U">The type to map to.</typeparam>
        /// <param name="selector">The function to transform the value.</param>
        /// <returns>A new Result with the transformed value if successful, otherwise the same error.</returns>
        public Result<U, ErrorCode> Map<U>(Func<T, U> selector)
        {
            return IsSuccess
                ? Result<U, ErrorCode>.Success(selector(Value.Value!))
                : Result<U, ErrorCode>.Failure(ErrorState);
        }

        /// <summary>
        /// Chains another operation that returns a Result, only executing if this Result is successful.
        /// </summary>
        /// <typeparam name="U">The type of the next operation's success value.</typeparam>
        /// <param name="next">The function to execute if this Result is successful.</param>
        /// <returns>The result of the next operation if this Result is successful, otherwise the same error.</returns>
        public Result<U, ErrorCode> AndThen<U>(Func<T, Result<U, ErrorCode>> next)
        {
            return IsSuccess
                ? next(Value.Value!)
                : Result<U, ErrorCode>.Failure(ErrorState);
        }

#nullable enable
        /// <summary>
        /// Gets the value if successful, otherwise returns null/default.
        /// </summary>
        /// <returns>The value if successful, otherwise null or default value.</returns>
        public T? ValueOrNull()
        {
            return IsSuccess ? Value.Value : default;
        }
#nullable disable

        /// <summary>
        /// Executes an action with the value if the Result is successful.
        /// </summary>
        /// <param name="action">The action to execute with the value.</param>
        /// <returns>The same Result instance for method chaining.</returns>
        public Result<T, ErrorCode> OnSuccess(Action<T> action)
        {
            if (IsSuccess)
                action(Value.Value!);
            return this;
        }

        /// <summary>
        /// Executes an action with the error if the Result is a failure.
        /// </summary>
        /// <param name="action">The action to execute with the error.</param>
        /// <returns>The same Result instance for method chaining.</returns>
        public Result<T, ErrorCode> OnFailure(Action<ErrorCode> action)
        {
            if (!IsSuccess)
                action(ErrorState);
            return this;
        }

        /// <summary>
        /// Matches the Result with two functions: one for success and one for failure.
        /// </summary>
        /// <typeparam name="U">The return type of both match functions.</typeparam>
        /// <param name="onSuccess">Function to execute if successful.</param>
        /// <param name="onFailure">Function to execute if failed.</param>
        /// <returns>The result of the appropriate match function.</returns>
        public U Match<U>(Func<T, U> onSuccess, Func<ErrorCode, U> onFailure)
        {
            return IsSuccess ? onSuccess(Value.Value!) : onFailure(ErrorState);
        }

        /// <summary>
        /// Tries to aggregate multiple Result instances.
        /// </summary>
        /// <param name="results">The enumerable of Result instances to aggregate.</param>
        /// <returns>The aggregated values in a result instance.</returns>
        public static Result<T[], ErrorCode> TryAll(IEnumerable<Result<T, ErrorCode>> results)
        {
            List<T> resultValueList = new List<T>();
            foreach (Result<T, ErrorCode> result in results)
            {
                // Deremine if any of the results is a failure
                // If is, return the failure and stop the aggregation
                // Otherwise, add the value to the result list and continue
                if (!result.IsSuccess)
                    return Result<T[], ErrorCode>.Failure(result.ErrorState);

                resultValueList.Add(result.Value.UnwrapOr(default));
            }
            return Result<T[], ErrorCode>.Success(resultValueList.ToArray());
        }
        #endregion
    }
}