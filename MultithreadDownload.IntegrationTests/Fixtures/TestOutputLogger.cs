using MultithreadDownload.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.IntegrationTests.Fixtures
{
    /// <summary>
    /// A logger implementation that routes output to xUnit's ITestOutputHelper.
    /// </summary>
    public class TestOutputLogger : IDownloadLogger
    {
        private readonly Xunit.Abstractions.ITestOutputHelper _output;

        public TestOutputLogger(Xunit.Abstractions.ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// Logs a message with the Debug log level.
        /// </summary>
        /// <param name="message">The debug message to log.</param>
        public void LogDebug(string message)
        {
            Log("DEBUG", message);
        }

        /// <summary>
        /// Logs a message with the Information log level.
        /// </summary>
        /// <param name="message">The information message to log.</param>
        public void LogInfo(string message)
        {
            Log("INFO", message);
        }

        /// <summary>
        /// Logs a message with the Warning log level.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        public void LogWarning(string message)
        {
            Log("WARNING", message);
        }

        /// <summary>
        /// Logs a message with the Error log level.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="exception">The exception associated with the error, if any.</param>
        public void LogError(string message, Exception? exception = null)
        {
            Log("ERROR", message);
            if (exception != null)
                Log("ERROR", exception.Message);
        }

        private void Log(string level, string message)
        {
            try
            {
                _output.WriteLine($"[{DateTime.Now}] {level}: {message}");
            }
            catch (InvalidOperationException ex)
            {
                // If the output is not available (e.g., test output is disposed), we can ignore it.
                Debug.WriteLine($"[Error] Failed to log message: {ex.Message}");
                Debug.WriteLine("The logger is not available, cannot log the message: " + message);
            }
        }
    }
}
