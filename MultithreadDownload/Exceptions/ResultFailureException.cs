using MultithreadDownload.Core.Errors;
using MultithreadDownload.Protocols.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MultithreadDownload.Exceptions
{
    public class ResultFailureException<ErrorCode> : Exception
    {
        public ErrorCode Error { get; }

        public ResultFailureException(ErrorCode errorCode)
            : base("The result is failed and an exception has been thrown out.")
        {
            this.Error = errorCode;
        }
    }
}
