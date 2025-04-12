using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Core
{
    /// <summary>
    /// An interface for monitoring download speed.
    /// </summary>
    public interface IDownloadSpeedMonitor
    {
        /// <summary>
        /// Starts the speed monitor.
        /// </summary>
        /// <param name="getDownloadedSize"></param>
        void Start(Func<long> getDownloadedSize);

        /// <summary>
        /// Stops the speed monitor.
        /// </summary>
        void Stop();

        /// <summary>
        /// Gets the current download speed which is automatically converted to any suitable unit.
        /// e.g. KiB/s, MiB/s, GiB/s, etc.
        /// </summary>
        event Action<string> SpeedUpdated;
    }
}
