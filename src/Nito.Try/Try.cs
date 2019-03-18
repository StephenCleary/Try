using System;
using System.Threading.Tasks;

namespace Nito
{
    /// <summary>
    /// Static factory methods for <see cref="Try{T}"/> types.
    /// </summary>
    public static class Try
    {
        /// <summary>
        /// Creates a wrapper for an exception.
        /// </summary>
        /// <param name="exception">The exception to wrap.</param>
        public static Try<T> FromException<T>(Exception exception) => Try<T>.FromException(exception);

        /// <summary>
        /// Creates a wrapper for a value.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        public static Try<T> FromValue<T>(T value) => Try<T>.FromValue(value);

        /// <summary>
        /// Executes the specified function, and wraps either the result or the exception.
        /// </summary>
        /// <param name="func">The function to execute.</param>
        public static Try<T> Create<T>(Func<T> func) => Try<T>.Create(func);

        /// <summary>
        /// Executes the specified function, and wraps either the result or the exception.
        /// </summary>
        /// <param name="func">The function to execute.</param>
        public static Task<Try<T>> Create<T>(Func<Task<T>> func) => Try<T>.Create(func);
    }
}
