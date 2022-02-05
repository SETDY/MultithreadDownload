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
        public static string ToDownloadRate(this int size)
        {

            String[] units = new String[] { "B", "KB", "MB", "GB", "TB", "PB" };
            double mod = 1024.0;
            int i = 0;
            double result = 0;
            while (size >= mod)
            {
                result = size / mod;//计算
                i++;
            }
            return Math.Round(result) + units[i];//将大小加上单位返回
        }
    }
}
