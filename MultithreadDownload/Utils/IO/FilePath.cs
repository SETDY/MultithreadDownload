using MultithreadDownload.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Utils.IO
{
    /// <summary>
    /// A class representing a file path, which may not necessarily exist in the file system.
    /// </summary>
    public class FilePath
    {
        /// <summary>
        /// The absolute path of the file, which includes the directory path and the file name.
        /// </summary>
        public string AbsolutePath => Path.Combine(this.ParentalDirectory.FullName, this.FileName);

        /// <summary>
        /// The dictory info of the file, which is the parent directory of the file.
        /// </summary>
        public DirectoryInfo ParentalDirectory { get; private set; }

        /// <summary>
        /// The file name of the file, which is the last part of the absolute path.
        /// </summary>
        public string FileName { get; private set; }

        public FilePath(string path) : this(Path.GetDirectoryName(path), Path.GetFileName(path)) { }

        public FilePath(string directoryPath, string fileName) : this(new DirectoryInfo(directoryPath), fileName) { }

        public FilePath(DirectoryInfo parentalDirectory, string fileName)
        {
            // Validate the directory path and file name
            if (parentalDirectory is null)
            {
                throw new NullReferenceException("The provided DirectoryInfo is null.");
            }
            if (!PathHelper.IsValidPath(fileName))
            {
                throw new ArgumentException("The provided file name is not valid.", nameof(fileName));
            }
            // Initialize the properties
            this.ParentalDirectory = parentalDirectory;
            this.FileName = fileName;
        }

        /// <summary>
        /// Determines whether the file at the specified absolute path exists.
        /// </summary>
        /// <returns>Return true if a file exists at the specified absolute path; otherwise, false.</returns>
        public bool IsExist()
        {
            // Return if the file exists at the specified absolute path
            return File.Exists(this.AbsolutePath);
        }

        /// <summary>
        /// Change the file name of the file path, which will update the absolute path accordingly.
        /// </summary>
        /// <param name="newFileName">The new file name</param>
        /// <exception cref="ArgumentException">The provided file name is not valid.</exception>
        public void ChangeFileName(string newFileName)
        {
            // Validate the new file name
            if (!PathHelper.IsValidPath(newFileName))
            {
                throw new ArgumentException("The provided file name is not valid.", nameof(newFileName));
            }
            // Update the file name
            this.FileName = newFileName;
        }

        /// <summary>
        /// Changes the current working directory to the specified directory.
        /// </summary>
        /// <param name="newDirectory">The target directory to switch to. Must not be null.</param>
        /// <exception cref="NullReferenceException">Thrown if <paramref name="newDirectory"/> is null.</exception>
        public void ChangeDirectory(DirectoryInfo newDirectory)
        {
            // Validate the new directory
            if (newDirectory is null)
            {
                throw new NullReferenceException("The provided DirectoryInfo is null.");
            }
            // Update the parental directory
            this.ParentalDirectory = newDirectory;
        }

        public FilePath EnsureUnique(IFileNameConflictResolver conflictResolver)
        {
            return conflictResolver.Resolve(this);
        }

        /// <summary>
        /// Returns a string that represents the absolute path of the current object.
        /// </summary>
        /// <returns>A string containing the absolute path represented by this instance.</returns>
        public override string ToString()
        {
            return this.AbsolutePath;
        }
    }
}
