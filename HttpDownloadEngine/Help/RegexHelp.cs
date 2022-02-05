using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MultithreadDownload.Help
{
    public static class RegexHelp
    {
        //public static bool IsUrl(string url)
        //{

        //}

        public static bool IsHttp(string url)
        {
            return Regex.IsMatch(url, "https?://");
        }
    }
}
