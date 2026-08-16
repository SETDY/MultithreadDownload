using MultithreadDownload.Downloads;
using MultithreadDownload.Threads.States;
using MultithreadDownload.Utils.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Threads.Core
{
    public class DownloadThreadContext
    {
        /// <summary>
        /// The id of the download thread, which is a unique identifier for each download thread.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// The path to the file segment that this download thread is responsible for downloading.
        /// </summary>
        public FilePath FileSegmentPath { get; }

        /// <summary>
        /// The start position of the file segment that this download thread is responsible for downloading, in bytes.
        /// </summary>
        public long StartPositon { get; }

        /// <summary>
        /// The end position of the file segment that this download thread is responsible for downloading, in bytes.
        /// </summary>
        public long EndPositon { get; }

        /// <summary>
        /// The current offset of the download thread, which indicates the current position of the download thread in the file segment that it is responsible for downloading, in bytes.
        /// </summary>
        public long CurrentPostion { get; private set; }

        /// <summary>
        /// The size of the each chunk that the download thread will download in each iteration untile the whole file segement is completed, in bytes.
        /// </summary>
        public long ChunkSize { get; }

        /// <summary>
        /// The total size of the file has be downloaded for this thread, in bytes.
        /// </summary>
        public long TotalDownloaded => CurrentPostion - StartPositon;

        /// <summary>
        /// The state of the download thread, which indicates the current status of the download thread.
        /// </summary>
        internal DownloadTaskStateMachine StateMachine { get; }

        public DownloadThreadContext(Guid id, FilePath fileSegementPath, long startPos, long endPos, long chunkSize)
        {
            // Initialize the thread context properties.
            this.Id = id;
            this.FileSegmentPath = fileSegementPath;
            this.StartPositon = startPos;
            this.EndPositon = endPos;
            this.CurrentPostion = startPos;
            this.ChunkSize = chunkSize;
            this.StateMachine = new DownloadTaskStateMachine();
        }

        /// <summary>
        /// Update the current position of the download thread, 
        /// which indicates the current offset of the download thread in the file segment that it is responsible for downloading.
        /// </summary>
        /// <param name="newPosition">
        /// The new position of the download thread
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If the new position is negative or out of the range of the file segment, an exception will be thrown to indicate that the new position is invalid.
        /// </exception>
        public void UpdateCurrentPosition(long newPosition)
        {
            // If the new position is negative or out of the range of the file segment, throw an exception to indicate that the new position is invalid.
            // Otherwise, update the current position of the download thread.
            if (newPosition < 0 || newPosition < StartPositon || newPosition > EndPositon)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(newPosition),
                    $"The new position must be non-negative and between {StartPositon} and {EndPositon}.");
            }
            this.CurrentPostion = newPosition;
        }

        /// <summary>
        /// Get the remaining bytes to download for the thread
        /// </summary>
        /// <returns>The remaining bytes to download</returns>
        public long GetRemainingBytesToDownload()
        {
            // Calculate the remaining bytes to download for the thread
            // BEWARE:
            // The byte that is at the CurrentPosition has not been downloaded yet,
            // so the remaining bytes to download should be calculated by the end position minus the current position plus 1
            return this.EndPositon - this.CurrentPostion + 1;
        }
    }
}
