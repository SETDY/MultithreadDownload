using MultithreadDownload.Protocols.Abstractions;
using System;

namespace MultithreadDownload.Exceptions
{
    public class DownloadContextInvaildException : Exception
    {
        public DownloadContextInvaildException()
    : base("The download context is invalid.")
        {
        }

        public DownloadContextInvaildException(IDownloadTarget downloadContext)
            : base($"The download context is invalid. The target path of it is {downloadContext.SavedPath}")
        {
        }

        public DownloadContextInvaildException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}