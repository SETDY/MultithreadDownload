using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.UnitTests.Utils
{
    public class ExtensionMethodsTests
    {
        #region ToSpeed()
        [Theory]
        [InlineData(0, "0 B/s")]
        [InlineData(500, "500 B/s")]
        [InlineData(1023, "1023 B/s")]
        public void ToSpeed_ShouldReturnBytes_WhenLessThan1KiB(long input, string expected)
        {
            var result = input.ToSpeed();
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(1024, "1 KiB/s")]
        [InlineData(1536, "1.5 KiB/s")]
        [InlineData(1048575, "1024 KiB/s")] // Just below 1 MiB
        public void ToSpeed_ShouldReturnKiB_WhenBetween1KiBAnd1MiB(long input, string expected)
        {
            var result = input.ToSpeed();
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(1048576, "1 MiB/s")]
        [InlineData(1572864, "1.5 MiB/s")]
        [InlineData(1073741823, "1024 MiB/s")] // Just below 1 GiB
        public void ToSpeed_ShouldReturnMiB_WhenBetween1MiBAnd1GiB(long input, string expected)
        {
            var result = input.ToSpeed();
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(1073741824, "1 GiB/s")]
        [InlineData(1610612736, "1.5 GiB/s")]
        [InlineData(1099511627776, "1024 GiB/s")] // 1 TiB
        public void ToSpeed_ShouldReturnGiB_When1GiBOrMore(long input, string expected)
        {
            var result = input.ToSpeed();
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-1048576)]
        public void ToSpeed_ShouldThrowArgumentOutOfRangeException_WhenNegativeNumbers(long input)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => input.ToSpeed());
        }

        [Fact]
        public void ToSpeed_ShouldHandleLongMaxValue()
        {
            long input = long.MaxValue;
            var result = input.ToSpeed();
            result.Should().Contain("GiB/s");
        }
        #endregion
    }
}
