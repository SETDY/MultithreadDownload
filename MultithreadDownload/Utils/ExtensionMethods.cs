using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Utils
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Convert the download speed from bytes to a human-readable format.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string ToSpeed(this long downloadSpeedAsBytes)
        {
            // Convert the download speed to a Gib/s
            if (downloadSpeedAsBytes >= 1024 * 1024 * 1024)
            {
                return $"{Math.Round((double)downloadSpeedAsBytes / (1024 * 1024 * 1024),2)} GiB/s";
            }
            // Convert the download speed to a Mib/s
            else if (downloadSpeedAsBytes >= 1024 * 1024)
            {
                return $"{Math.Round((double)downloadSpeedAsBytes / (1024 * 1024), 2)} MiB/s";
            }
            // Convert the download speed to a Kib/s
            else if (downloadSpeedAsBytes >= 1024)
            {
                return $"{Math.Round((double)downloadSpeedAsBytes / 1024, 2)} KiB/s";
            }
            throw new ArgumentOutOfRangeException("It is not a vaild input to convert it to a proper speed rate.");
        }

        public static bool IsIndexOutOfBounds(this int index, int count)
        {
            if(index > count - 1)//index是否大于数组项目的长度减一
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
