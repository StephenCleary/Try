using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Nito
{
    /// <summary>
    /// A wrapper for either an exception or a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    public sealed class Try<T>
    {
        /// <summary>
        /// Creates a wrapper for an exception.
        /// </summary>
        /// <param name="exception">The exception to wrap.</param>
        public static Try<T> FromException(Exception exception) => new Try<T>(exception, default!, true);

        /// <summary>
        /// Creates a wrapper for a value.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        public static Try<T> FromValue(T value) => new Try<T>(default, value, false);

        /// <summary>
        /// Executes the specified function, and wraps either the result or the exception.
        /// </summary>
        /// <param name="func">The function to execute.</param>
        public static Try<T> Create(Func<T> func)
        {
            _ = func ?? throw new ArgumentNullException(nameof(func));
            try
            {
                return FromValue(func());
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                return FromException(exception);
            }
        }

        /// <summary>
        /// Executes the specified function, and wraps either the result or the exception.
        /// </summary>
        /// <param name="func">The function to execute.</param>
        public static async Task<Try<T>> Create(Func<Task<T>> func)
        {
            _ = func ?? throw new ArgumentNullException(nameof(func));
            try
            {
                return FromValue(await func().ConfigureAwait(false));
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                return FromException(exception);
            }
        }

        /// <summary>
        /// Maps a wrapped value to another mapped value.
        /// If this instance is an exception, <paramref name="func"/> is not invoked, and this method returns a wrapper for that exception.
        /// If <paramref name="func"/> throws an exception, this method returns a wrapper for that exception.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the mapping.</typeparam>
        /// <param name="func">The mapping function. Exceptions from this method are captured and wrapped.</param>
        public Try<TResult> Map<TResult>(Func<T, TResult> func) => Bind(value => Try<TResult>.Create(() => func(value)));

        /// <summary>
        /// Maps a wrapped value to another mapped value.
        /// If this instance is an exception, <paramref name="func"/> is not invoked, and this method immediately returns a wrapper for that exception.
        /// If <paramref name="func"/> throws an exception, this method returns a wrapper for that exception.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the mapping.</typeparam>
        /// <param name="func">The mapping function. Exceptions from this method are captured and wrapped.</param>
        public Task<Try<TResult>> Map<TResult>(Func<T, Task<TResult>> func) => Bind(value => Try<TResult>.Create(async () => await func(value).ConfigureAwait(false)));

        /// <summary>
        /// Binds the wrapped value.
        /// If this instance is an exception, <paramref name="bind"/> is not invoked, and this method returns a wrapper for that exception.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the binding.</typeparam>
        /// <param name="bind">The binding function. Should not throw exceptions.</param>
        public Try<TResult> Bind<TResult>(Func<T, Try<TResult>> bind)
        {
            _ = bind ?? throw new ArgumentNullException(nameof(bind));
            return IsException ? Try<TResult>.FromException(_exception!) : bind(_value);
        }

        /// <summary>
        /// Binds the wrapped value.
        /// If this instance is an exception, <paramref name="bind"/> is not invoked, and this method immediately returns a wrapper for that exception.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the binding.</typeparam>
        /// <param name="bind">The binding function. Should not throw exceptions.</param>
        public async Task<Try<TResult>> Bind<TResult>(Func<T, Task<Try<TResult>>> bind)
        {
            _ = bind ?? throw new ArgumentNullException(nameof(bind));
            return IsException ? Try<TResult>.FromException(_exception!) : await bind(_value).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a method (Action) for the wrapped exception or value.
        /// </summary>
        /// <param name="whenException">The method to execute if this instance is an exception.</param>
        /// <param name="whenValue">The method to execute if this instance is a value.</param>
        public void Match(Action<Exception> whenException, Action<T> whenValue)
        {
            _ = whenException ?? throw new ArgumentNullException(nameof(whenException));
            _ = whenValue ?? throw new ArgumentNullException(nameof(whenValue));
            if (IsException)
            {
                whenException(_exception!);
            }
            else
            {
                whenValue(_value);
            }
        }

        /// <summary>
        /// Executes a method (Func) for the wrapped exception or value.
        /// </summary>
        /// <param name="whenException">The method to execute if this instance is an exception.</param>
        /// <param name="whenValue">The method to execute if this instance is a value.</param>
        public TResult Match<TResult>(Func<Exception, TResult> whenException, Func<T, TResult> whenValue)
        {
            _ = whenException ?? throw new ArgumentNullException(nameof(whenException));
            _ = whenValue ?? throw new ArgumentNullException(nameof(whenValue));
            return IsException ? whenException(_exception!) : whenValue(_value);
        }

        /// <summary>
        /// Enables LINQ support as a monad.
        /// </summary>
        public Try<TResult> Select<TResult>(Func<T, TResult> func) => Map(func);

        /// <summary>
        /// Enables LINQ support as a monad.
        /// </summary>
        public Try<TResult> SelectMany<TOther, TResult>(Func<T, Try<TOther>> bind, Func<T, TOther, TResult> project) => Bind(a => bind(a).Select(b => project(a, b)));

        /// <summary>
        /// Deconstructs this wrapper into two variables.
        /// </summary>
        /// <param name="exception">The wrapped exception, or <c>null</c> if this instance is a value.</param>
        /// <param name="value">The wrapped value, or <c>default</c> if this instance is an exception.</param>
        public void Deconstruct(out Exception? exception, out T value)
        {
            exception = _exception;
            value = _value;
        }

        /// <summary>
        /// Whether this instance is an exception.
        /// </summary>
        public bool IsException => _exception != null;

        /// <summary>
        /// Whether this instance is a value.
        /// </summary>
        public bool IsValue => !IsException;

        /// <summary>
        /// Gets the wrapped exception, if any. Returns <c>null</c> if this instance is not an exception.
        /// </summary>
        public Exception? Exception => _exception;

        /// <summary>
        /// Gets the wrapped value. If this instance is an exception, then that exception is (re)thrown.
        /// </summary>
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
        public T Value => IsException ? throw Rethrow() : _value;
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations

        /// <summary>
        /// A string representation, useful for debugging.
        /// </summary>
        public override string ToString() => IsException ? $"Exception: {_exception}" : $"Value: {_value}";

        private Try(Exception? exception, T value, bool isException)
        {
            if (isException)
            {
                _exception = exception ?? throw new ArgumentNullException(nameof(exception));
            }
            else
            {
                _value = value;
            }
        }

        private Exception Rethrow()
        {
            ExceptionDispatchInfo.Capture(_exception!).Throw();
            return _exception!;
        }

        private readonly Exception? _exception;
        private readonly T _value = default!;
    }
}
