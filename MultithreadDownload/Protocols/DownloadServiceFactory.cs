using MultithreadDownload.Core;
using MultithreadDownload.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Protocols
{
    /// <summary>
    /// This class is responsible for creating download services based on the provided context.
    /// <summary>
    public static class DownloadServiceFactory
    {
        /// <summary>
        /// Creates a download service based on the provided context.
        /// </summary>
        /// <param name="context">The download context which is used to indentify a download service and return<param name="context">
        /// <returns>The suitable download service.</returns>
        /// <exception cref="NotSupportedException">The context does not have a supported download service.</exception>
        public static IDownloadService CreateService(DownloadServiceType serviceType)
        {
            if (serviceType is DownloadServiceType.Http)
            {
                return new HttpDownloadService();
            }

            throw new NotSupportedException("The context does not have a supported download service.");
        }
    }
}
