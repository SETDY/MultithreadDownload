using System.IO;
using System.Text.RegularExpressions;

namespace MultithreadDownload.Utils
{
    public static class PathHelper
    {
        /// <summary>
        /// Automatically generates a unique file name if the specified file already exists.
        /// </summary>
        /// <param name="directoryPath">The path of directory which saves the file</param>
        /// <param name="fileName">The name of the file</param>
        /// <returns>A full file path of the file</returns>
        public static string GetUniqueFileName(string directoryPath, string fileName)
        {
            // If the file does not exist, return the original path
            string originalPath = Path.Combine(directoryPath, fileName);
            if (File.Exists(originalPath) == false)
            {
                return originalPath;
            }

            // If the file exists, generate a new name for the file by appending a number
            // e.g. TestFile (1).txt, TestFile (2).txt, etc.
            for (int i = 1; true; i++)
            {
                // Get the temporary file name
                string tempFileName =
                    Path.GetFileNameWithoutExtension(fileName) + $" ({i})" + Path.GetExtension(fileName);
                // Create the full path for the temporary file and check if it exists
                // If it does not exist, return the path
                string tempFullFilePath = Path.Combine(directoryPath, tempFileName);
                if (File.Exists(tempFullFilePath) == false)
                {
                    return tempFullFilePath;
                }
            }
        }

        /// <summary>
        /// Checks if the given path is valid.
        /// </summary>
        /// <param name="path">the path you want to check</param>
        /// <returns>Is the path valid of not</returns>
        public static bool IsValidPath(string path)
        {
            // If the path is a empty string  => not a valid path
            if ("".Equals(path)) { return false; }
            // Check the path is whether valid by using regex and system methods
            if (IsVaildPathByRegex(path) && IsVaildPathBySystem(path))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the given path is valid by using regex.
        /// </summary>
        /// <param name="path">the path you want to check</param>
        /// <returns>Is the path valid of not</returns>
        private static bool IsVaildPathByRegex(string path)
        {
            // Use regex to check if the path is valid
            Regex pathPattern = new Regex(@"^([a-zA-Z]:\\)?[^\/\:\*\?\""\<\>\|\,]*$");
            return pathPattern.Match(path).Success;
        }

        /// <summary>
        /// Checks if the given path is valid by using system methods.
        /// </summary>
        /// <param name="path">the path you want to check</param>
        /// <returns>Is the path valid of not</returns>
        private static bool IsVaildPathBySystem(string path)
        {
            // Check If the path is not null or empty and does not contain any invalid characters
            if (!string.IsNullOrWhiteSpace(path)) { return false; }
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 1) { return false; }
            try
            {
                // Try to get full path so that it can be validated by the system
                string fullPath = Path.GetFullPath(path);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}