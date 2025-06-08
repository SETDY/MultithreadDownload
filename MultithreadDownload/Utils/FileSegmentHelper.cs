using MultithreadDownload.Core.Errors;
using MultithreadDownload.Logging;
using System;
using System.IO;
using System.Linq;

namespace MultithreadDownload.Primitives
{
    /// <summary>
    /// The FileSegmentHelper class provides methods to handle file segmentation for multithreaded downloads.
    /// </summary>
    internal class FileSegmentHelper
    {
        /// <summary>
        /// Combine file segments into a single file after download completion safely.
        /// </summary>
        /// <param name="downloadTask">The download task containing the segmented files.</param>
        /// <returns>Whether the operation was successful.</returns>
        public static Result<bool> CombineSegmentsSafe(string[] fileSegmentPaths, ref FileStream finalFileStream)
        {
            // Validate the input parameters => the file segment paths and final file path must be valid
            // If not, Combine the file segments and return the result.
            if (fileSegmentPaths.Length == 0 || finalFileStream == null) { return Result<bool>.Failure("The file segment paths or final file path cannot be null."); }
            try
            {
                // If it is a single segment, just rename the file segment to the final file name
                // and return the result of the operation.
                if (HandleSingleSegment(fileSegmentPaths, ref finalFileStream, out Result<bool> success))
                    return success;
                Result<bool> result = CombineSegments(fileSegmentPaths, ref finalFileStream);
                if (!result.IsSuccess) { return Result<bool>.Failure($"Cannot combine file segments: {result.ErrorMessage}"); }
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Cannot combine file segments: {ex.Message}");
            }
            finally
            {
                // Clean up the file stream and delete the file segments
                // Prevent an unexpected exception when combining file segments
                // which is happened before CleanupFileStream() can be run in CombineSegments()
                CleanupFiles(fileSegmentPaths);
            }
        }

        #region Private Methods about implementation of CombineSegmentsSafe()

        /// <summary>
        /// Handles the case where there is only a single segment to download.
        /// </summary>
        /// <returns>Whether it is a single segment and be handled or not.</returns>
        private static bool HandleSingleSegment(string[] fileSegmentPaths, ref FileStream finalFileStream, out Result<bool> success)
        {
            // If it is not a single segment, return false and set success to true.
            // Otherwise, to save time, just rename the file segment to the final file name and return true.
            if (fileSegmentPaths.Length != 1)
            { 
                success = Result<bool>.Success(true);
                return false;
            }

            // Get the final file name and dispose file stream and delete the unwriten final file
            // to let the segment can be renamed to the final file name.
            string finalFileNamePath = finalFileStream.Name;
            CleanupFileStream(ref finalFileStream, new string[] { finalFileNamePath });
            try
            {
                File.Move(fileSegmentPaths[0], finalFileNamePath);
            }
            catch (Exception)
            {
                success = Result<bool>.Failure($"Cannot rename file segment: {fileSegmentPaths[0]} to final file: {finalFileNamePath}");
                return true;
            }
            success = Result<bool>.Success(true);
            return true;
        }

        /// <summary>
        /// Combines the segmented files into a single file after download completion.
        /// </summary>
        /// <param name="fileSegmentPaths">The paths of the segmented files.</param>
        /// <param name="finalFileStream">The final file stream to write to.</param>
        /// <returns>Whether the operation was successful.</returns>
        private static Result<bool> CombineSegments(string[] fileSegmentPaths, ref FileStream finalFileStream)
        {
            // Using the final file stream and enumerate through the threads to combine their segments.
            // After that, flush and close the final file stream.
            // If the whole process success, return success.
            // Ohterwise, return failure.
            // CleanupFileStream() has been used to clean up the file stream and delete the file segments
            // after the whole process is done which is nomatter success or failure.
            Result<bool> wholeProcessResult = Result<bool>.Success(true);
            foreach (string segmentPath in fileSegmentPaths)
            {
                Result<bool> result = CombineFileSegmentSafe(segmentPath, ref finalFileStream);
                if (!result.IsSuccess)
                {
                    wholeProcessResult = Result<bool>.Failure($"Cannot combine file segment: {segmentPath}");
                }
            }
            CleanupFileStream(ref finalFileStream, fileSegmentPaths);
            return wholeProcessResult;
        }

        /// <summary>
        /// Combines a file segment into the final file stream safely.
        /// </summary>
        /// <param name="threadFileSegmentPath">The path of a file segment to combine.</param>
        /// <param name="finalFileSegmentStream">The final file stream to write to.</param>
        /// <returns>Whether the operation was successful.</returns>
        private static Result<bool> CombineFileSegmentSafe(string threadFileSegmentPath, ref FileStream finalFileSegmentStream)
        {
            try
            {
                CombineFileSegment(threadFileSegmentPath, ref finalFileSegmentStream);
                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure($"Cannot combine file segment: {threadFileSegmentPath}");
            }
        }

        /// <summary>
        /// Combines a file segments into final file.
        /// </summary>
        /// <param name="threadFileSegmentPath">The path of a file segment to combine.</param>
        /// <param name="finalFileSegmentStream">The final file stream to write to.</param>
        private static void CombineFileSegment(string threadFileSegmentPath, ref FileStream finalFileSegmentStream)
        {
            // Read the file segment and write it to the final file
            // readFileBytesCount is the number of bytes read from the file segment
            // which is larger than 0 means the file segment is not empty
            int readFileBytesCount;
            byte[] readFileBytes = new byte[1024];

            FileStream threadFileSegmentStream = new FileStream(threadFileSegmentPath, FileMode.Open);
            do
            {
                readFileBytesCount = threadFileSegmentStream.Read(readFileBytes, 0, readFileBytes.Length);
                finalFileSegmentStream.Write(readFileBytes, 0, readFileBytesCount);
            }
            while (readFileBytesCount > 0);
            CleanupFileStream(ref threadFileSegmentStream, new string[] { threadFileSegmentPath });
        }

#nullable enable
        /// <summary>
        /// Performs cleanup operations for file streams and associated files.
        /// </summary>
        /// <param name="fileStream">The file stream to cleanup.</param>
        /// <param name="filePath">Array of file paths to delete.</param>
        private static void CleanupFileStream(ref FileStream fileStream, string?[] filePaths)
        {
            // Flush and close the file stream if it exists
            // Then attempt to delete all associated files
            // If deletion fails, silently continue
            if (fileStream != null)
            {
                fileStream.Flush();
                fileStream.Close();
            }
            try
            {
                if (filePaths != null)
                    CleanupFiles(filePaths);
            }
            catch (Exception)
            {
                DownloadLogger.LogError($"Failed to delete files: {string.Join(", ", filePaths)}");
                return;
            }
        }
#nullable disable

        /// <summary>
        /// Performs cleanup operations for associated files.
        /// </summary>
        /// <param name="filePath">The path of the file to delete.</param>
        private static void CleanupFiles(string[] filePaths)
        {
            // Attempt to delete all the associated files
            // If deletion fails, silently continue
            try
            {
                foreach (string filePath in filePaths)
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        #endregion Private Methods about implementation of CombineSegmentsSafe()

        /// <summary>
        /// Splits the file path into multiple segments based on the number of threads.
        /// </summary>
        /// <param name="threadCount">The number of threads to split the file into.</param>
        /// <param name="path">The main path of the file to split.</param>
        /// <returns>Whether the operation was successful.</returns>
        public static Result<string[]> SplitPaths(int maxThreads, string path)
        {
            // If the thread count is 0, return failure
            // If the file name is empty, return failure
            // Otherwise, create an array of strings to store the paths of the file segments
            // Set the paths to the array and return it
            if (maxThreads <= 0) { return Result<string[]>.Failure("The number of threads cannot be 0 oor negative."); }
            if ("".Equals(Path.GetFileName(path))) { return Result<string[]>.Failure("The file name cannot be empty."); }
            string[] resultPaths = new string[maxThreads];
            for (int i = 0; i < maxThreads; i++)
            {
                resultPaths[i] = Path.Combine(PathHelper.GetDirectoryNameSafe(path), $"{Path.GetFileNameWithoutExtension(path)}-{i}.downtemp");
            }
            return Result<string[]>.Success(resultPaths);
        }

        /// <summary>
        /// Splits the file size into multiple segments based on the number of segments.
        /// </summary>
        /// <param name="fileSize">The total size of the file to split.</param>
        /// <param name="segmentCount">The number of segments to split the file into.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Result<long[,]> CalculateFileSegmentRanges(long fileSize, int segmentCount)
        {
            // Validate the input parameters => the file size and segment count must be greater than zero
            if (fileSize <= 0 || segmentCount <= 0) { return Result<long[,]>.Failure("File size and segment count must be greater than zero."); }

            long[,] segments = new long[segmentCount, 2];
            long segmentSize = fileSize / segmentCount;
            long remainingBytes = fileSize % segmentCount;

            // Calculate the start and end positions of each segment
            // Then, add them to the segments array
            // The last segment will include the remaining bytes
            long currentStart = 0;
            for (int i = 0; i < segmentCount; i++)
            {
                long currentEnd = currentStart + segmentSize - 1;
                // Add remaining bytes to the last segment
                if (i == segmentCount - 1)
                {
                    currentEnd += remainingBytes;
                }
                segments[i, 0] = currentStart;
                segments[i, 1] = currentEnd;
                currentStart = currentEnd + 1;
            }
            return Result<long[,]>.Success(segments);
        }

        /// <summary>
        /// Deletes the specified segments from the file system.
        /// </summary>
        /// <param name="segementPaths">The paths of the segments to delete.</param>
        /// <returns>The result of the deletion operation.</returns>
        public static Result<bool, DownloadError> DeletSegements(string[] segementPaths)
        {
            // Validate the input parameters => the segment paths must not be null
            if (segementPaths == null)
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.NullReference, "Segment paths cannot be null."));
            // If the segment paths are empty, it means there are no segments to delete,
            // so, return success without doing anything.
            if (segementPaths.Length == 0)
                return Result<bool, DownloadError>.Success(true);
            try
            {
                // Attempt to delete the segment files if they exist
                segementPaths.ToList().ForEach(path =>
                {
                    if (File.Exists(path))
                        File.Delete(path);
                });
                return Result<bool, DownloadError>.Success(true);
            }
            // Catch specific exceptions to provide more detailed error messages
            catch (UnauthorizedAccessException ex)
            {
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.PermissionDenied, 
                    $"Unauthorized access while deleting segments: {ex.Message}"));
            }
            catch (IOException ex)
            {
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.DiskOperationFailed, 
                    $"IO error while deleting segments: {ex.Message}"));
            }
            catch (Exception ex)
            {
                return Result<bool, DownloadError>.Failure(DownloadError.Create(DownloadErrorCode.UnexpectedOrUnknownException, 
                    $"Unexpected error while deleting segments: {ex.Message}"));
            }
        }
    }
}