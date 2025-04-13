using System;

namespace MultithreadDownload.Exceptions
{
    public class UrlFileFileSizeIsNullOrZeroException : Exception
    {
        public UrlFileFileSizeIsNullOrZeroException()
            : base("此Url所指向的文件的大小为NULL或0,请确认此Url")
        {
        }

        public UrlFileFileSizeIsNullOrZeroException(string url)//指定错误消息
            : base($"{url}所指向的文件的大小为NULL或0,请确认{url}的正确")
        {
        }

        public UrlFileFileSizeIsNullOrZeroException(string message, Exception inner)//指定错误消息和内部异常信息
            : base(message, inner)
        {
        }
    }
}