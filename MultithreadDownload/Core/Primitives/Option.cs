using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Primitives
{
    /// <summary>
    /// Represents an optional value. Similar to Rust's Option or Nullable<T> for reference & value types.
    /// </summary>
    public readonly struct Option<T>
    {
#nullable enable
        /// <summary>
        /// The value contained in the Option if it has one; otherwise, null.
        /// </summary>
        private readonly T? _value;
#nullable disable

        /// <summary>
        /// Indicates whether the Option contains a value.
        /// </summary>
        public bool HasValue { get; }

        /// <summary>
        /// Gets the value contained in the Option if it exists; otherwise, throws an exception.
        /// </summary>
        public T Value => HasValue
            ? _value!
            : throw new InvalidOperationException("Option has no value.");

        /// <summary>
        /// Initializes a new instance of the <see cref="Option{T}"/> struct with no value.
        /// </summary>
        /// <param name="value">The value to wrap in the Option.</param>
        private Option(T value)
        {
            _value = value;
            HasValue = true;
        }

        /// <summary>
        /// Creates an Option containing a value.
        /// </summary>
        /// <param name="value">The value to wrap in the Option.</param>
        /// <returns>The instance of <see cref="Option{T}"/> containing the value.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the value is null.</exception>
        public static Option<T> Some(T value) =>
            value is null
                ? throw new ArgumentNullException(nameof(value), "Cannot wrap null in Some.")
                : new Option<T>(value);

        /// <summary>
        /// Creates an Option with no value (None).
        /// </summary>
        /// <returns>The instance of <see cref="Option{T}"/> with no value.</returns>
        public static Option<T> None() => new Option<T>();

        /// <summary>
        /// Matches the Option against two functions: one for when it has a value (Some) and one for when it does not (None).
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the match functions.</typeparam>
        /// <param name="onSome">When the Option has a value, this function is called with the value.</param>
        /// <param name="onNone">When the Option has no value, this function is called.</param>
        /// <returns>The result of the matched function.</returns>
        public TResult Match<TResult>(Func<T, TResult> onSome, Func<TResult> onNone) =>
            HasValue ? onSome(_value!) : onNone();

        /// <summary>
        /// Maps the value contained in the Option to a new type using the provided mapping function.
        /// </summary>
        /// <typeparam name="TResult">The type of the result after mapping.</typeparam>
        /// <param name="mapper">The function to apply to the value if it exists.</param>
        /// <returns>The Option containing the mapped value if it exists; otherwise, an Option with no value.</returns>
        public Option<TResult> Map<TResult>(Func<T, TResult> mapper) =>
            HasValue ? Option<TResult>.Some(mapper(_value!)) : Option<TResult>.None();

        /// <summary>
        /// Executes the provided function if the Option has a value, passing the value to it.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="next"></param>
        /// <returns></returns>
        public Option<TResult> AndThen<TResult>(Func<T, Option<TResult>> next) =>
            HasValue ? next(_value!) : Option<TResult>.None();

        /// <summary>
        /// Unwraps the value contained in the Option, returning the value if it exists; otherwise, returns a fallback value.
        /// </summary>
        /// <param name="fallback">The fallback value to return if the Option has no value.</param>
        /// <returns>The value if it exists; otherwise, the fallback value.</returns>
        public T UnwrapOr(T fallback) => HasValue ? _value! : fallback;

        /// <summary>
        /// Unwraps the value contained in the Option, returning the value if it exists; otherwise, throws an exception.
        /// </summary>
        /// <param name="message">The message to include in the exception if the Option has no value.</param>
        /// <returns>The value if it exists.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the Option has no value.</exception>
        public T UnwrapOrThrow(string? message = null) =>
            HasValue ? _value! : throw new InvalidOperationException(message ?? "No value present.");
    }
}

