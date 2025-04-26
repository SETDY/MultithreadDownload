using MultithreadDownload.Protocols;
using System;

namespace MultithreadDownload.Exceptions
{
    public class DownloadContextInvaildException : Exception
    {
        public DownloadContextInvaildException()
    : base("The download context is invalid.")
        {
        }

        public DownloadContextInvaildException(IDownloadContext downloadContext)
            : base($"The download context is invalid. The target path of is is {downloadContext}")
        {
        }

        public DownloadContextInvaildException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}