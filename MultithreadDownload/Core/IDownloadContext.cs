using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadDownload.Core
{
    /// <summary>
    /// Interface for download context.
    /// e.g. target path, progress reporter, cancellation token.
    /// </summary>
    public interface IDownloadContext
    {
        /// <summary>
        /// The target path where the downloaded file will be saved.
        /// </summary>
        public string TargetPath { get; set; }
    }
}
