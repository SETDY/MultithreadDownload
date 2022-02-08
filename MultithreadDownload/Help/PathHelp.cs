using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Help
{
    public static class PathHelp
    {
        /// <summary>
        /// 自动获取名称相撞的文件应该取得名称
        /// </summary>
        /// <param name="path">路径 (包含文件)</param>
        public static string AutomaticFileName(string path)
        {
            //判断是否有文件的名称相撞
            if (File.Exists(path) == true)
            {
                for (int i = 1; true; i++)
                {
                    //给予临时文件名
                    string temPath = path.Substring(0, path.LastIndexOf('.')) + $" ({i})" + path.Substring(path.LastIndexOf('.'));
                    //判断临时文件名是否有文件的名称与其相撞
                    if (File.Exists(temPath) == false)
                    {
                        //返回临时文件名
                        return temPath;
                    }
                }
            }
            else
            {
                return path;
            }
        }

        /// <summary>
        /// 使用正则表达式检测路径是否正确        
        /// </summary>
        /// <returns>是否正确</returns>
        public static bool IsRightForRegex(string path)
        {
            return RegexHelp.IsRightPath(path);
        }
    }
}
