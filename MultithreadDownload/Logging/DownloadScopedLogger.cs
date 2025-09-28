using MultithreadDownload.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Logging
{
#nullable enable
    /// <summary>
    /// Provides a scoped logger that includes task and thread context for download operations.
    /// </summary>
    public readonly struct DownloadScopedLogger
    {
        /// <summary>
        /// The task ID associated with the download operation, if any.
        /// </summary>
        private readonly Option<string> _taskId;

        /// <summary>
        /// The thread ID associated with the download operation, if any.
        /// </summary>
        private readonly Option<byte> _threadId;

        public DownloadScopedLogger(Option<string> taskId, Option<byte> threadId)
        {
            _taskId = taskId;
            _threadId = threadId;
        }

        /// <summary>
        /// Retrieves information about the context this logger.
        /// </summary>
        public (string, byte) GetContext()
        {
            // Return the context by unwrapping the options, providing default values if not set.
            return (_taskId.UnwrapOr("Undefined"), _threadId.UnwrapOr(255));
        }

        /// <summary>
        /// Logs a message with the Information log level.
        /// </summary>
        /// <param name="message">The information message to log.</param>
        public void LogInfo(string message)
        {
            string prefix = (this._taskId.HasValue ? $"[Task: {_taskId.Value}]" : "") + (this._threadId.HasValue ? $" [Thread: {_threadId.Value}]" : "");
            DownloadLogger.Current.LogInfo($"{prefix} {message}".Trim());
        }

        /// <summary>
        /// Logs a message with the Warning log level.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        public void LogWarning(string message)
        {
            string prefix = (this._taskId.HasValue ? $"[Task: {_taskId.Value}]" : "") + (this._threadId.HasValue ? $" [Thread: {_threadId.Value}]" : "");
            DownloadLogger.Current.LogWarning($"{prefix} {message}".Trim());
        }

        /// <summary>
        /// Logs a message with the Error log level.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="exception">The exception associated with the error, if any.</param>
        public void LogError(string message, Exception? exception = null)
        {
            string prefix = (this._taskId.HasValue ? $"[Task: {_taskId.Value}]" : "") + (this._threadId.HasValue ? $" [Thread: {_threadId.Value}]" : "");
            DownloadLogger.Current.LogError($"{prefix} {message}".Trim(), exception);
        }
    }
#nullable disable
}
