using MultithreadDownload.Core.Errors;
using MultithreadDownload.Protocols.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MultithreadDownload.Exceptions
{
    public class DownloadErrorException : Exception
    {
#nullable enable
        public DownloadError? Error { get; }
#nullable disable

        public DownloadErrorException()
            : base("There is a unhandled error during the download process.")
        {
        }

        public DownloadErrorException(DownloadError error)
    : base($"There is a unhandled error ({error.Code}) during the download process.")
        {
            this.Error = error;
        }

        public DownloadErrorException(IDownloadTarget taget, DownloadError error)
            : base($"There is a unhandled error ({error.Code}) during the download process for target: {taget.FilePath}")
        {
            this.Error = error;
        }

        public DownloadErrorException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
