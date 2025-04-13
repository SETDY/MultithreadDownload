namespace MultithreadDownload.Utils
{
    /// <summary>
    /// Represents the result of an operation, which can be either a success or a failure.
    /// Similar to the Result type in Rust.
    /// </summary>
    /// <typeparam name="T">The type of the value returned when the operation is successful.</typeparam>
    public class Result<T>
    {
        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool IsSuccess { get; }

#nullable enable

        /// <summary>
        /// Gets the value of the result if the operation was successful; otherwise, null.
        /// </summary>
        public T? Value { get; }

        /// <summary>
        /// Gets the error message if the operation failed; otherwise, null.
        /// </summary>
        public string? ErrorMessage { get; }

#nullable disable

        private Result(T value)
        {
            IsSuccess = true;
            Value = value;
            ErrorMessage = null;
        }

        private Result(string errorMessage)
        {
            IsSuccess = false;
            ErrorMessage = errorMessage;
            Value = default;
        }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <param name="value">The value to return if the operation is successful.</param>
        /// <returns>A <see cref="Result{T}"/> instance indicating success.</returns>
        public static Result<T> Success(T value) => new Result<T>(value);

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        /// <param name="errorMessage">The error message describing why the operation failed.</param>
        /// <returns>A <see cref="Result{T}"/> instance indicating failure.</returns>
        public static Result<T> Failure(string errorMessage) => new Result<T>(errorMessage);
    }
}