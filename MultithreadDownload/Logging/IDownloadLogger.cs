using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Logging
{
    /// <summary>
    /// Defines logging methods for the download library.
    /// </summary>
    public interface IDownloadLogger
    {
        /// <summary>
        /// Logs a message with the Information log level.
        /// </summary>
        /// <param name="message">The information message to log.</param>
        void LogInfo(string message);

        /// <summary>
        /// Logs a message with the Warning log level.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        void LogWarning(string message);

#nullable enable
        /// <summary>
        /// Logs a message with the Error log level.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="exception">The exception associated with the error, if any.</param>
        void LogError(string message, Exception? exception = null);
#nullable disable
    }
}
