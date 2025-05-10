using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Utils
{
    /// <summary>
    /// Represents an optional value.
    /// </summary>
    /// <remarks>
    /// If the value is present, it can be accessed through the <see cref="Value"/> property.
    /// </remarks>
    /// <typeparam name="T">The type of the value.</typeparam>
    public class Option<T>
    {
        /// <summary>
        /// Indicates whether the value is present.
        /// </summary>
        public bool HasValue { get; }

#nullable enable
        /// <summary>
        /// The value if present; otherwise, null.
        /// </summary>
        public T? Value { get; }
#nullable disable

        /// <summary>
        /// Initializes a new instance of the <see cref="Option{T}"/> class with no value.
        /// </summary>
        private Option() => HasValue = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Option{T}"/> class with a value.
        /// </summary>
        /// <param name="value">The value to be wrapped.</param>
        private Option(T value)
        {
            HasValue = true;
            Value = value;
        }

        #region Auxiliary Methods
        /// <summary>
        /// Unwraps the value if present; otherwise, throws an <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <returns>The value of the <see cref="Option{T}"/>.</returns>
        /// <exception cref="InvalidOperationException">The value is not present.</exception>
        public T Unwrap() =>
            HasValue ? Value! : throw new InvalidOperationException($"Unwrap failed because the HasValue property is false.");

        /// <summary>
        /// Unwraps the value if present; otherwise, returns the provided default value.
        /// </summary>
        /// <param name="defaultValue">The default value for the value of <see cref="Option{T}"/>.</param>
        /// <returns>The value of the <see cref="Option{T}"/> if present; otherwise, the default value.</returns>
        public T UnwrapOr(T defaultValue) =>
            HasValue ? Value! : defaultValue;
        #endregion

        #region Static Methods => Factory Methods
        /// <summary>
        /// Creates an <see cref="Option{T}"/> with a value.
        /// </summary>
        /// <param name="value">The value to be wrapped.</param>
        /// <returns>The instance of <see cref="Option{T}"/> with the value.</returns>
        public static Option<T> Some(T value) => new Option<T>(value);

        /// <summary>
        /// Creates an <see cref="Option{T}"/> with no value.
        /// </summary>
        /// <returns>The instance of <see cref="Option{T}"/> with no value.</returns>
        public static Option<T> None() => new Option<T>();
        #endregion
    }

}
