using MultithreadDownload.Logging;
using MultithreadDownload.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Core.Errors
{
    /// <summary>
    /// Represents an error that can occur during the download process, including an error code, category, and a readable message.
    /// </summary>
    public sealed class DownloadError
    {
        /// <summary>
        /// The error code that represents the specific type of error encountered during the download process.
        /// </summary>
        public DownloadErrorCode Code { get; }

        /// <summary>
        /// This property categorizes the error into a broader category for easier handling and logging.
        /// </summary>
        public DownloadErrorCategory Category { get; }

        /// <summary>
        /// The readable error message that describes the error in a user-friendly way.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadError"/> class with the specified error code, category, and message.
        /// </summary>
        /// <param name="code">The error code that represents the specific type of error.</param>
        /// <param name="category">The category of the error, which helps in grouping similar errors together.</param>
        /// <param name="message">The readable error message that describes the error.</param>
        public DownloadError(DownloadErrorCode code, DownloadErrorCategory category, string message)
        {
            Code = code;
            Category = category;
            Message = message;
            DownloadLogger.LogError($"Error occurred: {ToString()}");
        }

        /// <summary>
        /// Returns a string representation of the <see cref="DownloadError"/> instance, including the category, code, and message.
        /// </summary>
        /// <returns>The string representation of the error.</returns>
        public override string ToString()
        {
            return $"[{Category}] {Code}: {Message}";
        }

        /// <summary>
        /// Creates a new instance of <see cref="DownloadError"/> with the specified error code and message.
        /// </summary>
        /// <param name="code">The error code that represents the specific type of error.</param>
        /// <param name="message">The readable error message that describes the error.</param>
        /// <returns>The created <see cref="DownloadError"/> instance.</returns>
        public static DownloadError Create(DownloadErrorCode code, string message) =>
            new(code, code.GetCategory(), message);
    }
}
