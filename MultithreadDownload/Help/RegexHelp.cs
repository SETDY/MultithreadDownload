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

        public static bool IsHttp(string url)
        {
            return Regex.IsMatch(url, "https?://");
        }

        public static bool IsRightPath(string path)
        {
            if(path != "")
            {
                Regex regex = new Regex(@"^([a-zA-Z]:\\)?[^\/\:\*\?\""\<\>\|\,]*$");
                Match match = regex.Match(path);//是否匹配
                if(match.Success == false)
                {
                    return false;
                }
                regex = new Regex(@"^[^\/\:\*\?\""\<\>\|\,]+$");
                match = regex.Match(path);//是否匹配
                if (match.Success == false)
                {
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
