using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Logging
{

    /// <summary>
    /// Provides a default static logger instance and registration methods.
    /// </summary>
    public static class DownloadLogger
    {
        private static IDownloadLogger _current = new NullDownloadLogger();

        /// <summary>
        /// Gets or sets the logger used by the library.
        /// </summary>
        public static IDownloadLogger Current
        {
            get => _current;
            set => _current = value ?? new NullDownloadLogger();
        }

        public static void LogInfo(string message) => Current.LogInfo(message);
        public static void LogWarning(string message) => Current.LogWarning(message);
#nullable enable
        public static void LogError(string message, Exception? exception = null) => Current.LogError(message, exception);
#nullable disable
    }
}
