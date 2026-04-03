using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Utils.IO
{
    /// <summary>
    /// The resolver for file name conflicts, which is used to resolve the conflict when a file with the same name already exists in the target directory.
    /// </summary>
    public interface IFileNameConflictResolver
    {
        public FilePath Resolve(FilePath filePath);
    }

    public class IncrementalFileNameConflictResolver : IFileNameConflictResolver
    {
        public FilePath Resolve(FilePath filePath)
        {
            // If the file does not exist, return the original path
            if (!File.Exists(filePath.AbsolutePath))
            {
                return filePath;
            }

            // If the file exists, generate a new name for the file by appending number  in sequence
            // e.g. TestFile (1).txt, TestFile (2).txt, etc.
            for (int i = 1; true; i++)
            {
                // Create the new full path for a possible file name and check if it exists
                // If it does not exist, return the new FilePath with the new full path
                string newFullPath = Path.Combine(
                    filePath.ParentalDirectory.FullName, 
                    $"{Path.GetFileNameWithoutExtension(filePath.FileName)} ({i}) {Path.GetExtension(filePath.AbsolutePath)}");
                if (!File.Exists(newFullPath))
                {
                    return new FilePath(newFullPath);
                }
            }
        }
    }
}
