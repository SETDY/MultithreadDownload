using MultithreadDownload.Downloads;
using MultithreadDownload.Utils.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Threads
{
    public class DownloadThreadContext
    {
        /// <summary>
        /// The id of the download thread, which is a unique identifier for each download thread.
        /// </summary>
        public int Id { get; }

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
        public long CurrentPostion { get; set; }

        /// <summary>
        /// The total size of the file has be downloaded for this thread, in bytes.
        /// </summary>
        public long TotalDownloaded => CurrentPostion - StartPositon;

        /// <summary>
        /// The state of the download thread, which indicates the current status of the download thread.
        /// </summary>
        public DownloadState State { get; set; }

        public DownloadThreadContext(int id, FilePath fileSegementPath, long startPos, long endPos)
        {
            // Initialize the thread context properties.
            this.Id = id;
            this.FileSegmentPath = fileSegementPath;
            this.StartPositon = startPos;
            this.EndPositon = endPos;
            this.CurrentPostion = startPos;
            this.State = DownloadState.Waiting;
        }
    }
}
