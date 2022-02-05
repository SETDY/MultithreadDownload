using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Help
{
    public static class IntExtension
    {
        /// <summary>
        /// 转换为下载速率
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string ToDownloadRate(this long fileSize)
        {
            if (fileSize < 0)
            {
                return "0 b/s";
            }
            else if (fileSize >= 1024 * 1024 * 1024) //文件大小大于或等于1024MB
            {
                return $"{Math.Round((double)fileSize / (1024 * 1024 * 1024),2)} GiB/s";
            }
            else if (fileSize >= 1024 * 1024) //文件大小大于或等于1024KB
            {
                return $"{Math.Round((double)fileSize / (1024 * 1024), 2)} MiB/s";
            }
            else if (fileSize >= 1024) //文件大小大于等于1024bytes
            {
                return $"{Math.Round((double)fileSize / 1024, 2)} KiB/s";
            }
            else
            {
                return $"{fileSize} b/s";
            }
        }
    }
}
