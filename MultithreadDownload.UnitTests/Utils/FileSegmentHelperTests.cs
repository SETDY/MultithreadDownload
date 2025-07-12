using FluentAssertions;
using MultithreadDownload.Core.Errors;
using System.Runtime.InteropServices;

namespace MultithreadDownload.UnitTests.Utils
{
    public class FileSegmentHelperTests
    {
        #region GetFileSegments()

        [Fact]
        public void GetFileSegments_ShouldDivideFileIntoEqualSegments()
        {
            // Arrange
            long fileSize = 100;
            int segmentCount = 4;

            // Act
            Result<long[,], DownloadError> result = FileSegmentHelper.CalculateFileSegmentRanges(fileSize, segmentCount);

            // Assert => Whether the processing is successful.
            result.IsSuccess.Should().BeTrue();

            // Assert => Whether the result is correct.
            result.Value.Value.Should().BeEquivalentTo(new long[,]
            {
                {0,  24},
                {25, 49},
                {50, 74},
                {75, 99}
            });
        }

        [Fact]
        public void GetFileSegments_ShouldHandleRemainingBytesInLastSegment()
        {
            // Arrange
            long fileSize = 103;
            int segmentCount = 4;

            // Act
            Result<long[,], DownloadError> result = FileSegmentHelper.CalculateFileSegmentRanges(fileSize, segmentCount);

            // Assert => Whether the processing is successful.
            result.IsSuccess.Should().BeTrue();

            // Assert => Whether the result is correct.
            result.Value.Value.Should().BeEquivalentTo(new long[,]
            {
                {0, 24},
                {25, 49},
                {50, 74},
                {75, 102}
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-54)]
        public void GetFileSegments_ShouldReturnError_WhenFileSizeIsZeroOrNegative(long fileSize)
        {
            // Arrange
            int segmentCount = 4;

            // Act
            Result<long[,], DownloadError> result = FileSegmentHelper.CalculateFileSegmentRanges(fileSize, segmentCount);

            // Assert => Whether the processing is failed.
            result.IsSuccess.Should().BeFalse();

            // Assert => Whether the result is correct.
            result.Value.HasValue.Should().BeFalse();

            // Assert => Whether the error state is correct.
            result.ErrorState.Should().NotBeNull();

            // Assert => Whether the error category is correct.
            result.ErrorState.Category.Should().Be(DownloadErrorCategory.Unexpected);

            // Assert => Whether the error code is correct.
            result.ErrorState.Code.Should().Be(DownloadErrorCode.ArgumentOutOfRange);
        }

        [Fact]
        public void GetFileSegments_ShouldReturnError_WhenSegmentCountIsZero()
        {
            // Arrange
            long fileSize = 100;
            int segmentCount = 0;

            // Act
            Result<long[,], DownloadError> result = FileSegmentHelper.CalculateFileSegmentRanges(fileSize, segmentCount);

            // Assert => Whether the processing is failed.
            result.IsSuccess.Should().BeFalse();

            // Assert => Whether the result's value is correct.
            result.Value.HasValue.Should().BeFalse();

            // Assert => Whether the error state is correct.
            result.ErrorState.Should().NotBeNull();

            // Assert => Whether the error category is correct.
            result.ErrorState.Category.Should().Be(DownloadErrorCategory.Unexpected);

            // Assert => Whether the error code is correct.
            result.ErrorState.Code.Should().Be(DownloadErrorCode.ArgumentOutOfRange);
        }

        [Fact]
        public void GetFileSegments_ShouldReturnError_WhenSegmentCountIsNegative()
        {
            // Arrange
            long fileSize = 100;
            int segmentCount = -54;

            // Act
            Result<long[,], DownloadError> result = FileSegmentHelper.CalculateFileSegmentRanges(fileSize, segmentCount);

            // Assert => Whether the processing is failed.
            result.IsSuccess.Should().BeFalse();

            // Assert => Whether the result's vaule is correct.
            result.Value.HasValue.Should().BeFalse();

            // Assert => Whether the error state is correct.
            result.ErrorState.Should().NotBeNull();

            // Assert => Whether the error category is correct.
            result.ErrorState.Category.Should().Be(DownloadErrorCategory.Unexpected);

            // Assert => Whether the error code is correct.
            result.ErrorState.Code.Should().Be(DownloadErrorCode.ArgumentOutOfRange);
        }

        #endregion GetFileSegments()

        #region CombineSegmentsSafe()

        [Fact]
        public void CombineSegmentsSafe_ShouldCombineSegmentsSuccessfully()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            string segment1Path = Path.Combine(tempDir, "segment1.tmp");
            string segment2Path = Path.Combine(tempDir, "segment2.tmp");
            string finalFilePath = Path.Combine(tempDir, "finalFile.txt");

            string content1 = "Hello, ";
            string content2 = "World!";

            // Create segment files with sample content
            File.WriteAllText(segment1Path, content1);
            File.WriteAllText(segment2Path, content2);

            FileStream finalFileStream = new FileStream(finalFilePath, FileMode.CreateNew);
            string[] segments = new[] { segment1Path, segment2Path };
            try
            {
                // Act
                var result = FileSegmentHelper.CombineSegmentsSafe(segments, finalFileStream);

                // Assert => Whether the processing is successful.
                result.IsSuccess.Should().BeTrue("because the segments should combine successfully");

                // Assert => Whether the result is correct.
                File.Exists(finalFilePath).Should().BeTrue("because the combined file should be created");
                File.Exists(segment1Path).Should().BeFalse("because the segment files should be cleaned up");
                File.Exists(segment2Path).Should().BeFalse("because the segment files should be cleaned up");

                string combinedContent = File.ReadAllText(finalFilePath);
                combinedContent.Should().Be(content1 + content2, "because the combined file content should match the segments");
            }
            finally
            {
                // Cleanup testing directory
                if (File.Exists(finalFilePath))
                    File.Delete(finalFilePath);

                // Cleanup testing directory
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void CombineSegmentsSafe_ShouldFail_WhenNoSegmentsProvided()
        {
            // Arrange
            string finalFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            FileStream finalFileStream = new FileStream(finalFilePath, FileMode.CreateNew);

            // Act
            Result<bool, DownloadError> result = FileSegmentHelper.CombineSegmentsSafe(Array.Empty<string>(), finalFileStream);

            // Assert => Whether the processing is failed.
            result.IsSuccess.Should().BeFalse("because no segments were provided");
            result.ErrorState.Should().NotBeNull();

            // Cleanup testing files
            if (finalFileStream != null)
                finalFileStream.Dispose();
            if (File.Exists(finalFilePath))
                File.Delete(finalFilePath);
        }

        #endregion CombineSegmentsSafe()

        #region SplitPaths()

        /// <summary>
        /// Test for SplitPaths() method to ensure it returns failure when thread count is zero or negative.
        /// </summary>
        /// <param name="maxThreads">The maximum number of threads.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void SplitPaths_ShouldReturnFailure_WhenThreadCountIsZeroOrNegative(int maxThreads)
        {
            // Act
            Result<string[], DownloadError> result = FileSegmentHelper.SplitPaths(maxThreads, "UserData\\Downloads\\file.zip");

            // Assert => Whether the processing is failed.
            result.IsSuccess.Should().BeFalse();
            result.ErrorState.Category.Should().Be(DownloadErrorCategory.Unexpected);
            result.ErrorState.Code.Should().Be(DownloadErrorCode.ArgumentOutOfRange);
        }

        /// <summary>
        /// Test for SplitPaths() method to ensure it returns failure when the path is a directory.
        /// </summary>
        [Fact]
        public void SplitPaths_ShouldReturnFail_WhenPathIsDirectory()
        {
            // Arrange
            int maxThreads = 4;
            string directoryPath = Path.Combine(Path.GetTempPath(), "TestDirectory") + Path.DirectorySeparatorChar;

            // Act
            Result<string[], DownloadError> result = FileSegmentHelper.SplitPaths(maxThreads, directoryPath);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorState.Category.Should().Be(DownloadErrorCategory.Unexpected);
            result.ErrorState.Code.Should().Be(DownloadErrorCode.ArgumentOutOfRange);
        }

        /// <summary>
        /// Test for SplitPaths() method to ensure it returns the correct paths based on the number of threads.
        /// </summary>
        /// <param name="maxThreads">The number of threads to split the file into.</param>
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        public void SplitPaths_ShouldReturnCorrectPaths_WhenInputIsValid(int maxThreads)
        {
            // Arrange
            string testFilePath = Path.Combine("UserData", "Downloads", "file.zip");

            // Act
            Result<string[], DownloadError> result = FileSegmentHelper.SplitPaths(maxThreads, testFilePath);

            // Assert => Whether the processing is successful.
            result.IsSuccess.Should().BeTrue();
            string[] valueOfResult = result.Value.UnwrapOrThrow();
            valueOfResult.Should().HaveCount(maxThreads);
            for (int i = 0; i < valueOfResult.Length; i++)
            {
                valueOfResult[i].Should().Be(Path.Combine("UserData", "Downloads", $"file-{i}.downtemp"));
            }
        }

        /// <summary>
        /// Test for SplitPaths() method to ensure it handles paths without an extension correctly.
        /// </summary>
        [Fact]
        public void SplitPaths_ShouldHandlePathWithoutExtension()
        {
            // Arrange
            int maxThreads = 2;
            string testFilePath = Path.Combine("testDir1", "data", "download", "myfile");

            // Act
            Result<string[], DownloadError> result = FileSegmentHelper.SplitPaths(maxThreads, testFilePath);

            // Assert => Whether the processing is successful.
            result.IsSuccess.Should().BeTrue();
            string[] valueOfResult = result.Value.UnwrapOrThrow();
            valueOfResult.Should().NotBeNullOrEmpty();
            valueOfResult.Should().HaveCount(2);
            valueOfResult[0].Should().EndWith("myfile-0.downtemp");
            valueOfResult[1].Should().EndWith("myfile-1.downtemp");
        }

        /// <summary>
        /// Test for SplitPaths() method to ensure it handles root directory correctly.
        /// </summary>
        /// <remarks>
        /// Since this test is specific to Windows, it checks if the OS is Windows before executing.
        /// </remarks>
        [Fact]
        public void SplitPaths_ShouldHandleRootDirectory()
        {
            // Check if the OS is not Windows, as the test is specific to Windows paths
            // Wchich is not supported in Unix-like systems
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.True(true);
            }

            // Arrange
            int maxThreads = 1;
            string testFilePath = "C:\\file.iso";

            // Act
            Result<string[], DownloadError> result = FileSegmentHelper.SplitPaths(maxThreads, testFilePath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            string[] valueOfResult = result.Value.UnwrapOrThrow();
            valueOfResult.Should().NotBeNullOrEmpty();
            valueOfResult.Should().HaveCount(1);
            valueOfResult[0].Should().Be("C:\\file-0.downtemp");
        }

        [Fact]
        public void SplitPaths_ShouldWorkWithNestedUnixStylePaths()
        {
            // Arrange
            int maxThreads = 2;
            string path = "home/user/downloads/file.tar.gz";

            // Act
            Result<string[], DownloadError> result = FileSegmentHelper.SplitPaths(maxThreads, path);

            // Assert
            result.IsSuccess.Should().BeTrue();
            string[] valueOfResult = result.Value.UnwrapOrThrow();
            valueOfResult.Should().NotBeNullOrEmpty();
            valueOfResult.Should().HaveCount(2);
            // Use Path.Combine to ensure corss platform compatibility
            valueOfResult[0].Should().Be(Path.Combine("home", "user", "downloads", "file.tar-0.downtemp"));
            valueOfResult[1].Should().Be(Path.Combine("home", "user", "downloads", "file.tar-1.downtemp"));
        }

        [Fact]
        public void SplitPaths_ShouldGenerateLargeNumberOfPaths()
        {
            // Arrange
            int maxThreads = 1000;
            string path = Path.Combine("UserData", "Downloads", "bigfile.bin");

            // Act
            Result<string[], DownloadError> result = FileSegmentHelper.SplitPaths(maxThreads, path);

            // Assert
            result.IsSuccess.Should().BeTrue();
            string[] valueOfResult = result.Value.UnwrapOrThrow();
            valueOfResult.Should().NotBeNullOrEmpty();
            valueOfResult.Should().HaveCount(1000);
            for (int i = 0; i < valueOfResult.Length; i++)
            {
                valueOfResult[i].Should().EndWith($"bigfile-{i}.downtemp");
            }
        }

        #endregion SplitPaths()
    }
}