using MultithreadDownload.Tasks;
using MultithreadDownload.Threading;
using MultithreadDownload.Threads;
using MultithreadDownload.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.IO
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
        public static Result<bool> CombineSegmentsSafe(DownloadTask downloadTask)
        {
            // Check if the download task is null
            // If not, Combine the file segments and return the result.
            if (downloadTask == null) { return Result<bool>.Failure("Download task is null."); }
            try
            {
                Result<bool> result = CombineSegments(downloadTask);
                if (!result.IsSuccess) { return Result<bool>.Failure($"Cannot combine file segments: {result.ErrorMessage}"); }
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Cannot combine file segments: {ex.Message}");
            }
        }

        /// <summary>
        /// Combines the segmented files into a single file after download completion.
        /// </summary>
        /// <param name="downloadTask">The download task containing the segmented files.</param>
        /// <returns>Whether the operation was successful.</returns>
        private static Result<bool> CombineSegments(DownloadTask downloadTask)
        {
            // Create the final file stream and enumerate through the threads to combine their segments.
            // After that, flush and close the final file stream.
            // If the whole process success, return success.
            // Otherwise, invoke the FailureProcessOfFileStream method to handle the failure process.
            // Then, return failure.
            FileStream finalFileStream = new FileStream(downloadTask.DownloadContext.TargetPath, FileMode.Open);
            foreach (DownloadThread thread in downloadTask.DownloadThreadManager.GetThreads())
            {
                Result<bool> result = CombineFileSegmentSafe(thread, thread.FileSegmentPath, ref finalFileStream);
                if(!result.IsSuccess)
                {
                    FailureProcessOfFileStream(ref finalFileStream,
                        downloadTask.DownloadThreadManager.GetThreads().Select(x => x.FileSegmentPath).ToArray());
                    return Result<bool>.Failure($"Cannot combine file segment: {thread.FileSegmentPath}");
                }
            }
            FailureProcessOfFileStream(ref finalFileStream, downloadTask.DownloadThreadManager.GetThreads().Select(x => x.FileSegmentPath).ToArray());
            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Combines a file segment into the final file stream safely.
        /// </summary>
        /// <param name="thread">A reference to the download thread.</param>
        /// <param name="threadFileSegmentPath">The path of a file segment to combine.</param>
        /// <param name="finalFileSegmentStream">The final file stream to write to.</param>
        /// <returns>Whether the operation was successful.</returns>
        private static Result<bool> CombineFileSegmentSafe(IDownloadThread thread,
            string threadFileSegmentPath, ref FileStream finalFileSegmentStream)
        {
            try
            {
                CombineFileSegment(thread, threadFileSegmentPath, ref finalFileSegmentStream);
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
        /// <param name="thread">A reference to the download thread.</param>
        /// <param name="threadFileSegmentPath">The path of a file segment to combine.</param>
        /// <param name="finalFileSegmentStream">The final file stream to write to.</param>
        private static void CombineFileSegment(IDownloadThread thread, 
            string threadFileSegmentPath, ref FileStream finalFileSegmentStream)
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
            FailureProcessOfFileStream(ref threadFileSegmentStream, threadFileSegmentPath);
        }

        /// <summary>
        /// Handles the failure process of a file stream.
        /// </summary>
        /// <param name="fileStream">The file stream to handle.</param>
        /// <param name="filePath">The path of the file to delete.</param>
        private static void FailureProcessOfFileStream(ref FileStream fileStream, string[] fileSegementPaths)
        {
            // If the file stream is not null, flush and close it then delete the file
            // If the process fails, just return
            if (fileStream != null)
            {
                fileStream.Flush();
                fileStream.Close();
            }
            try
            {
                foreach (string filePath in fileSegementPaths)
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// Handles the failure process of a file stream.
        /// </summary>
        /// <param name="fileStream">The file stream to handle.</param>
        /// <param name="filePath">The path of the file to delete.</param>
        private static void FailureProcessOfFileStream(ref FileStream fileStream, string fileSegementPaths)
        {
            // If the file stream is not null, flush and close it then delete the file
            // If the process fails, just return
            if (fileStream != null)
            {
                fileStream.Flush();
                fileStream.Close();
            }
            try
            {
                File.Delete(fileSegementPaths);
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// Splits the file path into multiple segments based on the number of threads.
        /// </summary>
        /// <param name="threadCount">The number of threads to split the file into.</param>
        /// <param name="path">The main path of the file to split.</param>
        /// <returns>Whether the operation was successful.</returns>
        public static Result<string[]> SplitPaths(int maxThreads, string path)
        {
            // If the thread count is 0, return failure
            // Otherwise, create an array of strings to store the paths of the file segments
            // Set the paths to the array and return it
            if (maxThreads <= 0) { return Result<string[]>.Failure("The number of threads cannot be 0 oor negative."); }
            string[] resultPaths = new string[maxThreads];
            for (int i = 0; i < maxThreads; i++)
            {
                resultPaths[i] = Path.Combine(PathHelper.GetDirectoryNameSafe(path), $"{Path.GetFileNameWithoutExtension(path)}-{i}.downtemp");
            }
            return Result<string[]>.Success(resultPaths);
        }

        /// <summary>
        /// Splits the file size into multiple segments based on the number of threads.
        /// </summary>
        /// <param name="threadCount"></param>
        /// <param name="eachThreadSize"></param>
        /// <returns></returns>
        public static Result<long[]> SplitPosition(int maxThreads, long eachThreadSize)
        {
            // If the thread count or each thread size is 0, return failure
            // Otherwise, create an array of long to store the positions of the file segments
            // Set the positions to the array and return it
            if (maxThreads <= 0 || eachThreadSize <= 0) { return Result<long[]>.Failure("The number of threads or eachThreadSize cannot be 0."); }
            long[] downloadPositions = new long[maxThreads];
            for (int i = 0; i < maxThreads; i++)
            {
                downloadPositions[i] = eachThreadSize * i;
            }
            return Result<long[]>.Success(downloadPositions);
        }

        /// <summary>
        /// Splits the file size into multiple segments based on the number of threads.
        /// </summary>
        /// <param name="maxThreads">The maximum number of threads to split the file into.</param>
        /// <param name="totalFileSize">The total size of the file to split.</param>
        /// <param name="remainingSize">The remaining size of the file after splitting.</param>
        /// <returns>The size of each thread excluding the remaining size.</returns>
        public static Result<long> SplitSize(int maxThreads, long totalFileSize, out Result<long> remainingSize)
        {
            // If the thread count or total file size is 0, return failure
            // Otherwise, calculate the size of each thread and the remaining size then return it
            if (maxThreads <= 0 || totalFileSize <= 0)
            {
                remainingSize = Result<long>.Failure("The number of threads or total file size cannot be 0.");
                return Result<long>.Failure("The number of threads or total file size cannot be 0.");
            }
            long eachSize = totalFileSize / maxThreads;
            remainingSize = Result<long>.Success(totalFileSize % maxThreads);
            return Result<long>.Success(eachSize);
        }
    }
}
