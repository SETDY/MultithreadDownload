using System;

namespace MultithreadDownload.Exceptions
{
    public class UrlCanNotConnectionException : Exception
    {
        public UrlCanNotConnectionException()
            : base("此Url无法连接,请确认是否输入正确")
        {
        }

        public UrlCanNotConnectionException(string url)//指定错误消息
            : base($"此Url无法连接,请确认{url}的正确")
        {
        }

        public UrlCanNotConnectionException(string message, Exception inner)//指定错误消息和内部异常信息
            : base(message, inner)
        {
        }
    }
}