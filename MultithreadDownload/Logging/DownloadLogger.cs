using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Logging
{
#nullable enable
    /// <summary>
    /// Provides a default static logger instance and registration methods.
    /// </summary>
    public static class DownloadLogger
    {
        /// <summary>
        /// The current logger instance used for logging messages.
        /// </summary>
        private static IDownloadLogger _current = new NullDownloadLogger();

        /// <summary>
        /// AsyncLocal storage for task ID.
        /// </summary>
        private static AsyncLocal<Guid?> _taskID = new AsyncLocal<Guid?>();

        /// <summary>
        /// AsyncLocal storage for thread ID.
        /// </summary>
        private static AsyncLocal<int?> _threadID = new AsyncLocal<int?>();

        /// <summary>
        /// Gets or sets the current logger instance.
        /// </summary>
        public static IDownloadLogger Current
        {
            get => _current;
            set => _current = value ?? new NullDownloadLogger();
        }

        /// <summary>
        /// Logs a message with the Information log level.
        /// </summary>
        /// <param name="message">The information message to log.</param>
        public static void LogInfo(string message) => Current.LogInfo(FormatMessage(message));

        /// <summary>
        /// Logs a message with the Warning log level.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        public static void LogWarning(string message) => Current.LogWarning(FormatMessage(message));

        /// <summary>
        /// Logs a message with the Error log level.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="exception">The exception associated with the error, if any.</param>
        public static void LogError(string message, Exception? exception = null) => Current.LogError(FormatMessage(message), exception);

        /// <summary>
        /// Formats the log message with task and thread context if available.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <returns>The formatted message.</returns>
        private static string FormatMessage(string message)
        {
            var task = _taskID.Value != null ? $"[Task: {_taskID.Value}]" : string.Empty;
            var thread = _threadID.Value != null ? $" [Thread: {_threadID.Value}]" : string.Empty;
            return $"{task}{thread} {message}".Trim();
        }

        /// <summary>
        /// Sets a task context for logging.
        /// </summary>
        public static IDisposable BeginTaskScope(Guid taskId)
        {
            _taskID.Value = taskId;
            return new LoggerScope(() => _taskID.Value = null);
        }

        /// <summary>
        /// Sets a thread context for logging.
        /// </summary>
        public static IDisposable BeginThreadScope(int threadId)
        {
            _threadID.Value = threadId;
            return new LoggerScope(() => _threadID.Value = null);
        }

        /// <summary>
        /// Creates a context-aware logger for a specific task and thread.
        /// </summary>
        public static DownloadScopedLogger For(string? taskId = null, int? threadId = null) => new DownloadScopedLogger(taskId, threadId);

        /// <summary>
        /// Represents a logger scope
        /// </summary>
        private class LoggerScope : IDisposable
        {
            /// <summary>
            /// Action to execute when the scope is disposed
            /// </summary>
            private readonly Action _onDispose;

            /// <summary>
            /// Initializes a new instance of the <see cref="LoggerScope"/> class with a dispose action.
            /// </summary>
            /// <param name="onDispose"></param>
            public LoggerScope(Action onDispose) => _onDispose = onDispose;

            /// <summary>
            /// Disposes the logger scope
            /// </summary>
            public void Dispose() => _onDispose();
        }
    }
#nullable disable
}
