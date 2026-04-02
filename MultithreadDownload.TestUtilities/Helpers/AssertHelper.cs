using MultithreadDownload.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.TestUtilities.Helpers
{
    /// <summary>
    /// A Class that contains various Assert helper methods for unit tests.
    /// </summary>
    public class AssertHelper
    {
        /// <summary>
        /// The constant defining the acceptable error range in bytes for download speed assertions.
        /// </summary>
        public const uint DOWNLOAD_SPEED_BYTES_ERROR_RANGE = 50;

        /// <summary>
        /// Validates that the specified number of bytes is within the allowable range defined by the maximum absolute
        /// error.
        /// </summary>
        /// <remarks>
        /// Use this method to ensure that a byte value does not deviate from an expected value
        /// by more than the specified maximum absolute error. This is useful for scenarios where minor variations are
        /// acceptable, such as in tolerance-based comparisons.
        /// </remarks>
        /// <param name="bytes">The value, in bytes, to validate against the permitted range.</param>
        /// <param name="maxAbsoluteError">The maximum absolute difference, in bytes, allowed from the specified value.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if the value of <paramref name="bytes"/> is outside the inclusive range of [bytes - maxAbsoluteError,
        /// bytes + maxAbsoluteError].
        /// </exception>
        public static void InRangeByBytes(uint bytes, uint maxAbsoluteError)
        {
            long lowerLimit = bytes - maxAbsoluteError;
            long upperLimit = bytes + maxAbsoluteError;
            if (bytes < lowerLimit || bytes > upperLimit)
            {
                throw new ArgumentOutOfRangeException($"Value {bytes} is not in range [{lowerLimit}, {upperLimit}]");
            }
        }

        /// <summary>
        /// Validates that the delta bytes per unit time of the specified formatted speed is within the allowable range
        /// defined by the maximum absolute error.
        /// </summary>
        /// <remarks>
        /// The valid range is calculated as [DeltaBytesPerUnitTime - maxAbsoluteError,
        /// DeltaBytesPerUnitTime + maxAbsoluteError]. Use this method to ensure that a measured or calculated speed
        /// falls within an acceptable tolerance.
        /// </remarks>
        /// <param name="formattedSpeed">The formatted speed to validate. The delta bytes per unit time of this value is checked against the
        /// acceptable range.</param>
        /// <param name="maxAbsoluteError">The maximum permitted deviation, in bytes per unit time, from the expected value. Must be a non-negative
        /// value.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if the delta bytes per unit time of the formatted speed is outside the range defined by the maximum
        /// absolute error.
        /// </exception>
        public static void InRangeByFormattedSpeed(FormattedSpeed formattedSpeed, uint maxAbsoluteError)
        {
            long lowerLimit = formattedSpeed.DeltaBytesPerUnitTime - maxAbsoluteError;
            long upperLimit = formattedSpeed.DeltaBytesPerUnitTime + maxAbsoluteError;
            if (formattedSpeed.DeltaBytesPerUnitTime < lowerLimit || formattedSpeed.DeltaBytesPerUnitTime > upperLimit)
            {
                throw new ArgumentOutOfRangeException($"Value {formattedSpeed.DeltaBytesPerUnitTime} is not in range [{lowerLimit}, {upperLimit}]");
            }
        }


    }
}
