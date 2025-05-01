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
            : base($"The download context is invalid. The target path of it is {downloadContext.TargetPath}")
        {
        }

        public DownloadContextInvaildException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}