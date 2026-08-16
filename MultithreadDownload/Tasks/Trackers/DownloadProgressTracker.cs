using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Tasks.Trackers
{
    /// <summary>
    /// The class that is responsible for tracking the download progress of a download task.
    /// </summary>
    public sealed class DownloadProgressTracker
    {
        /// <summary>
        /// The total size of the file that is being downloaded, in bytes.
        /// </summary>
        public long TotalSize { get; }

        /// <summary>
        /// The total number of bytes that have been downloaded so far. <br/>
        /// This is updated by the download threads as they download the file segments.
        /// </summary>
        public long TotalDownloaded { get; private set; }

        /// <summary>
        /// Whether the download progress can be tracked.
        /// </summary>
        /// <remarks>
        /// This will happen when the total size of the file is zero, which may occur when the content length is not known or cannot be determined.
        /// </remarks>
        public bool CanTrackProgress => TotalSize == 0;

        /// <summary>
        /// The download progress percentage of the file that is being downloaded.
        /// </summary>
        public double Percentage
        {
            get
            {
                // Define a threshold to trigger the overwrite precentage caculation
                // for preventing the error of the progress percentage is rounded to 100%
                // but the download is not completed yet,
                // which may cause the progress bar to be full but the download is still in progress.
                const double threshold = 0.1;
                const double overwritedProgress = 99.9;
                double comlpeteProgress = TotalSize == 0 ? 0 : (double)TotalDownloaded / TotalSize * 100;
                if ((100 - comlpeteProgress) < threshold)
                {
                    // If the download progress is close to 100% but not yet completed,
                    // return the overwrited progress percentage to prevent the error of the progress percentage being rounded to 100%.
                    return overwritedProgress;
                }
                else
                {
                    // Round the download progress percentage to 2 decimal places for better display and return it.
                    return Math.Round(comlpeteProgress, 2);
                }
            }
        }

        #nullable enable
        /// <summary>
        /// The event that is triggered when the download progress is changed.
        /// </summary>
        /// <remarks>
        /// The event handler takes two parameters: the total downloaded byte count and the download progress percentage. <br/>
        /// </remarks>
        public event Action<long, double>? ProgressChanged;
        #nullable disable

        public DownloadProgressTracker(long totalSize)
        {
            // Validate the total size parameter to ensure it is zero or greater, as a negative total size does not make sense in the context of a download.
            if (totalSize < 0)
            {
                throw new ArgumentException("The total size must be zero or greater.", nameof(totalSize));
            }

            // Initialize the TotalSize property
            TotalSize = totalSize;
        }

        /// <summary>
        /// Reports an incremental number of bytes that have been downloaded and updates the progress accordingly.
        /// </summary>
        /// <remarks>
        /// This method updates the total downloaded bytes and triggers the ProgressChanged event
        /// to notify subscribers of the new progress. <br/>
        /// Use this method to report progress in download.
        /// operations.
        /// </remarks>
        /// <param name="deltaBytes">The number of bytes to add to the total downloaded count. Must be zero or greater.</param>
        /// <exception cref="ArgumentException">Thrown if deltaBytes is less than zero.</exception>
        internal void Report(long deltaBytes)
        {
            if (deltaBytes < 0)
            {
                throw new ArgumentException("The delta bytes must be zero or greater.", nameof(deltaBytes));
            }
            // Update the total downloaded byte count by adding the specified number of bytes
            this.TotalDownloaded += deltaBytes;
            // Invoke the ProgressChanged event to notify subscribers of the updated progress
            this.ProgressChanged?.Invoke(this.TotalDownloaded, Percentage);
        }
    }
}
