using System.IO;

namespace MultithreadDownload.Primitives
{
    public static class PathHelper
    {
        /// <summary>
        /// Get the directory name of the specified path safely.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <remarks>
        /// Since <code>Path.GetDirectoryName(path)</code> will return null when path is rooted,
        /// this method will pack the method to prevent null reference exception.
        /// Therefore, the method will return the path itself if it is a rooted path
        /// </remarks>
        public static string GetDirectoryNameSafe(string path)
        {
            return Path.GetDirectoryName(path) ?? path;
        }

        /// <summary>
        /// Checks if the given path is valid.
        /// </summary>
        /// <param name="path">the path you want to check</param>
        /// <returns>Is the path valid of not</returns>
        public static bool IsValidPath(string path)
        {
            // Check If the path including the file name is not null or empty and
            // does not contain any invalid characters
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0 ||
                Path.GetFileName(path).IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

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