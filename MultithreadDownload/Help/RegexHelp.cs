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
            return IsMatch(url, "https?://");
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

        /// <summary>
        /// 验证是否是URL链接
        /// </summary>
        /// <param name="str">指定字符串</param>
        /// <returns></returns>
        public static bool IsUrl(string str)
        {
            string pattern = @"^(https?|ftp|file|ws)://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?$";
            return IsMatch(pattern, str);
        }

        /// <summary>
        /// 判断一个字符串，是否匹配指定的表达式(区分大小写的情况下)
        /// </summary>
        /// <param name="expression">正则表达式</param>
        /// <param name="str">要匹配的字符串</param>
        /// <returns></returns>
        public static bool IsMatch(string expression, string str)
        {
            Regex reg = new Regex(expression);
            if (string.IsNullOrEmpty(str))
                return false;
            return reg.IsMatch(str);
        }
    }
}
