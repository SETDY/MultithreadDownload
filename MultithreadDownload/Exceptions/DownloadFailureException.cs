using MultithreadDownload.Core.Errors;
using MultithreadDownload.Protocols.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MultithreadDownload.Exceptions
{
    public class DownloadFailureException: Exception
    {
        public DownloadFailureInfo FailureInfo { get; }

        public DownloadFailureException()
            : base("There is a unhandled error during the download process.")
        {
        }

        public DownloadFailureException(IDownloadTarget taget, DownloadFailureInfo failureInfo)
            : base($"There is a unhandled error ({failureInfo.Error.Code}) during the download process for target: {taget.FilePath}")
        {
            this.FailureInfo = failureInfo;
        }

        public DownloadFailureException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
