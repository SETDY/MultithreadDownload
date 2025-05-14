using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Logging
{
    /// <summary>
    /// A logger that does nothing. Use this when logging is not desired.
    /// </summary>
    public class NullDownloadLogger : IDownloadLogger
    {
        /// <summary>
        /// Logs a message with the Information log level.
        /// </summary>
        /// <param name="message">The information message to log.</param>
        /// <remarks>
        /// This method does nothing.
        /// </remarks>
        public void LogInfo(string message) { }

        /// <summary>
        /// Logs a message with the Warning log level.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        /// <remarks>
        /// This method does nothing.
        /// </remarks>
        public void LogWarning(string message) { }

#nullable enable
        /// <summary>
        /// Logs a message with the Error log level.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="exception">The exception associated with the error, if any.</param>
        /// <remarks>
        /// This method does nothing.
        /// </remarks>
        public void LogError(string message, Exception? exception = null) { }
#nullable disable
    }
}
