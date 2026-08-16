using MultithreadDownload.Protocols.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MultithreadDownload.Schedulers
{
    public class DownloadSegmentScheduler<TTarget> where TTarget : IDownloadTarget
    {
        public TTarget Target { get; }

        public long CurrentSegmentPosition { get; private set; } = 0;

        private long? BaseSegmentLength { get; }

        public DownloadSegmentScheduler(TTarget target, byte maxParallelThreadsCount)
        {
            // Validate the maximum number of parallel threads count
            if (maxParallelThreadsCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxParallelThreadsCount),
                    $"The maximum number of parallel threads must be greater than 0. Actual value: {maxParallelThreadsCount}");
            }

            // Initialize the properties
            this.Target = target;
            this.BaseSegmentLength = target.ContentLength / maxParallelThreadsCount;
        }

        public bool TryGetNextSegment(out (long, long) segementRange)
        {
            // If the download target does not support range requests,
            // we can only download the content in a single thread without byte range specifications.
            // In this case, we will return the whole content as a single segment for the first time,
            // and return false for subsequent calls to indicate that there are no more segments to download.
            if (!this.Target.SupportsRange)
            {
                if (CurrentSegmentPosition == 0)
                {
                    segementRange = (0, this.Target.ContentLength ?? long.MaxValue);
                    CurrentSegmentPosition = segementRange.Item2;
                    return true;
                }
                else
                {
                    segementRange = (0, 0);
                    return false;
                }
            }
            // Otherwise, Since the download target has supported range requests, we can specify byte ranges for each thread.
            // If there are free segments to download, we will return the next segment range for the current thread to download,
            // and update the current segment position accordingly.
            if (CurrentSegmentPosition < this.Target.ContentLength)
            {
                long startPositon = CurrentSegmentPosition + 1;
                long endPosition = Math.Min(CurrentSegmentPosition + BaseSegmentLength.Value, this.Target.ContentLength.Value);
                CurrentSegmentPosition = endPosition;

                segementRange = (startPositon, endPosition);
                return true;
            }
            // However, if there are no more segments to download, we will try to allocate the remaining segment of
            // current downloading threads to the new thread.

        }
