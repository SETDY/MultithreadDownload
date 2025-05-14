using MultithreadDownload.Logging;
using System;
using System.Collections.Generic;
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
        /// Logs a message with the Information log level.
        /// </summary>
        /// <param name="message">The information message to log.</param>
        public void LogInfo(string message)
        {
            _output.WriteLine($"INFO: {message}");
        }

        /// <summary>
        /// Logs a message with the Warning log level.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        public void LogWarning(string message)
        {
            _output.WriteLine($"WARN: {message}");
        }

        /// <summary>
        /// Logs a message with the Error log level.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="exception">The exception associated with the error, if any.</param>
        public void LogError(string message, Exception? exception = null)
        {
            _output.WriteLine($"ERROR: {message}");
            if (exception != null)
                _output.WriteLine(exception.ToString());
        }
    }
}
