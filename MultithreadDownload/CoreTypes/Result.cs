using System;
using System.Collections.Generic;

namespace MultithreadDownload.Utils
{
    /// <summary>
    /// Represents the result of an operation, which can be either a success or a failure.
    /// Similar to the Result type in Rust.
    /// </summary>
    /// <typeparam name="T">The type of the value returned when the operation is successful.</typeparam>
    /// <typeparam name="E">The type of the error message returned when the operation fails.</typeparam>
    public class Result<T, E>
    {
        #region Properties
        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets the value of the result if the operation was successful; otherwise, null. <br/>
        /// This property is of type <see cref="Option{T}"/> to indicate that the operation/property may or may not contain a value.
        /// </summary>
        public Option<T> SuccessValue { get; }

        /// <summary>
        /// Gets the error message if the operation failed; otherwise, null.
        /// </summary>
        public Option<E> FailureReason { get;}
        #endregion

        #region Constructors

        /// <summary>
        /// The constructor for successful result.
        /// </summary>
        /// <param name="value"></param>
        private Result(T value)
        {
            IsSuccess = true;
            SuccessValue = Option<T>.Some(value);
            FailureReason = default!;
        }

        /// <summary>
        /// The constructor for failed result.
        /// </summary>
        /// <param name="failureReason"></param>
        private Result(E failureReason)
        {
            IsSuccess = false;
            SuccessValue = default;
            FailureReason = Option<E>.Some(failureReason);
        }

        #endregion

        /// <summary>
        /// Maps the successful value to a new value using the provided mapper function.
        /// </summary>
        /// <typeparam name="U">The type of the new value.</typeparam>
        /// <param name="mapper">The function to map the success value.</param>
        /// <returns>The new result with the mapped value.</returns>
        /// <remarks>
        /// If the foregoing operation is failed, the mapper function will not be executed, 
        /// and the result with a original failure reason will just be returned.
        /// </remarks>
        public Result<U, E> Map<U>(Func<T, U> mapper)
        {
            // If the operation was successful, apply the mapper function to the success value.
            // Otherwise, return the failure reason.
            // Since the above check has make sure that we give the value to the mapper function must be non-null,
            // we can use the null-forgiving operator to suppress the warning.
            return IsSuccess ? Result<U, E>.Success(mapper(SuccessValue.Value!)) : 
                Result<U, E>.Failure(FailureReason.Value!);
        }

        /// <summary>
        /// Maps the failed reason to a new value using the provided mapper function.
        /// </summary>
        /// <typeparam name="U">The type of the new value.</typeparam>
        /// <param name="mapper">The function to map the failed value.</param>
        /// <returns>The new result with the mapped value.</returns>
        /// <remarks>
        /// If the foregoing operation is successful, the mapper function will not be executed, 
        /// and the result with a originally successful value will just be returned.
        /// </remarks>
        public Result<T, F> MapFailure<F>(Func<E, F> mapper)
        {
            // If the operation was failed, apply the mapper function to the failure reason.
            // Otherwise, return a result with original success value.
            // Since the above check has make sure that we give the value to the mapper function must be non-null,
            // we can use the null-forgiving operator to suppress the warning.
            return IsSuccess ? Result<T, F>.Success(SuccessValue.Value!) : 
                Result<T, F>.Failure(mapper(FailureReason.Value!));
        }

        /// <summary>
        /// Executes the provided function if the operation was successful.
        /// </summary>
        /// <typeparam name="U">The type of the new value.</typeparam>
        /// <param name="next">The provided function to execute if the operation was successful.</param>
        /// <returns>The result of the next operation.</returns>
        /// <remarks>
        /// If the foregoing operation is failed, the next function will not be executed and <br/>
        /// a result with the foregoing failure reason will just be returned.
        /// </remarks>
        public Result<U, E> AndThen<U>(Func<T, Result<U, E>> next)
        {
            // If the operation was successful, apply the next function to the success value.
            // Otherwise, return a result with original failed reasion.
            // Since the above check has make sure that we give the value to the next function must be non-null,
            // we can use the null-forgiving operator to suppress the warning.
            return IsSuccess ? next(SuccessValue.Value!) : Result<U, E>.Failure(FailureReason.Value!);
        }

        /// <summary>
        /// Executes the appropriate action based on whether the operation was successful or failed.
        /// </summary>
        /// <param name="onSuccess">The action to execute if the operation was successful.</param>
        /// <param name="onFailure">The action to execute if the operation was failed.</param>
        public void MatchFailure(Action<E> onFailure)
        {
            if (!IsSuccess)
                onFailure(FailureReason.Value);
        }

        /// <summary>
        /// Executes the appropriate action based on whether the operation was successful or failed.
        /// </summary>
        /// <param name="onSuccess">The action to execute if the operation was successful.</param>
        /// <param name="onFailure">The action to execute if the operation was failed.</param>
        public void Match(Action<T> onSuccess, Action<E> onFailure)
        {
            if (!IsSuccess)
                onFailure(FailureReason.Value);

        }

        /// <summary>
        /// Executes the appropriate function based on whether the operation was successful or failed,
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="onSuccess">The function to execute if the operation was successful.</param>
        /// <param name="onFailure">The function to execute if the operation was failed.</param>
        /// <returns>The result of the executed function.</returns>
        /// <remarks>
        /// This method is designed to be used when you want to return a value based on Result<![CDATA[<]]>T, E>, <br/>
        /// which supports chaining operations or handling errors in a functional style.
        /// </remarks>
        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<E, TResult> onFailure)
        {
            // If the operation was successful, execute the onSuccess function with the success value.
            // Otherwise, execute the onFailure function with the failure reason.
            // After the above process, return TResult of the executed function.
            return IsSuccess ? onSuccess(SuccessValue.Value) : onFailure(FailureReason.Value);
        }

        /// <summary>
        /// Executes the provided function for each item in the collection and returns a result with an array of successful values.
        /// </summary>
        /// <typeparam name="TIn">The type of the input items.</typeparam>
        /// <typeparam name="TOut">The type of the output items.</typeparam>
        /// <typeparam name="Ex">The type of the error message.</typeparam>
        /// <param name="items">The collection of items to process.</param>
        /// <param name="operation">The function to execute for each item.</param>
        /// <returns>The result with an array of successful values or the failure reason.</returns>
        /// <remarks>
        /// Since this method will return a failure which does not include any sucessful values 
        /// when there has any failure for running operation, <br/>
        /// you need to save your values if you want to have it when there is a failure.
        /// </remarks>
        public static Result<TOut[], Ex> TryAll<TIn, TOut, Ex>(IEnumerable<TIn> items, Func<TIn, Result<TOut, Ex>> operation)
        {
            // Do the operation for each item in the collection and collect the results.
            List<TOut> results = new List<TOut>();

            foreach (var item in items)
            {
                Result<TOut, Ex> result = operation(item);
                if (!result.IsSuccess)
                    return Result<TOut[], Ex>.Failure(result.FailureReason.Value!);

                results.Add(result.SuccessValue.Value!);
            }

            return Result<TOut[], Ex>.Success(results.ToArray());
        }

        #region Static Methods => Factory Methods

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <param name="value">The value to return if the operation is successful.</param>
        /// <returns>A <see cref="Result{T}"/> instance indicating success.</returns>
        public static Result<T, E> Success(T value) => new Result<T, E>(value);

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        /// <param name="errorMessage">The error message describing why the operation failed.</param>
        /// <returns>A <see cref="Result{T}"/> instance indicating failure.</returns>
        public static Result<T, E> Failure(E failureReason) => new Result<T, E>(failureReason);
        #endregion
    }
}