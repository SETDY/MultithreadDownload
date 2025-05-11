using FluentAssertions;
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
            Result<long[,]> result = FileSegmentHelper.CalculateFileSegmentRanges(fileSize, segmentCount);

            // Assert => Whether the processing is successful.
            result.IsSuccess.Should().BeTrue();

            // Assert => Whether the result is correct.
            result.Value.Should().BeEquivalentTo(new long[,]
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
            Result<long[,]> result = FileSegmentHelper.CalculateFileSegmentRanges(fileSize, segmentCount);

            // Assert => Whether the processing is successful.
            result.IsSuccess.Should().BeTrue();

            // Assert => Whether the result is correct.
            result.Value.Should().BeEquivalentTo(new long[,]
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
        public void GetFileSegments_ShouldThrowException_WhenFileSizeIsZeroOrNegative(long fileSize)
        {
            // Arrange
            int segmentCount = 4;

            // Act
            Result<long[,]> result = FileSegmentHelper.CalculateFileSegmentRanges(fileSize, segmentCount);

            // Assert => Whether the processing is failed.
            result.IsSuccess.Should().BeFalse();

            // Assert => Whether the result is correct.
            result.Value.Should().BeNull();

            // Assert => Whether the error message is correct.
            result.ErrorMessage.Should().Be("File size and segment count must be greater than zero.");
        }

        [Fact]
        public void GetFileSegments_ShouldThrowException_WhenSegmentCountIsZero()
        {
            // Arrange
            long fileSize = 100;
            int segmentCount = 0;

            // Act
            Result<long[,]> result = FileSegmentHelper.CalculateFileSegmentRanges(fileSize, segmentCount);

            // Assert => Whether the processing is failed.
            result.IsSuccess.Should().BeFalse();

            // Assert => Whether the result is correct.
            result.Value.Should().BeNull();

            // Assert => Whether the error message is correct.
            result.ErrorMessage.Should().Be("File size and segment count must be greater than zero.");
        }

        [Fact]
        public void GetFileSegments_ShouldThrowException_WhenSegmentCountIsNegative()
        {
            // Arrange
            long fileSize = 100;
            int segmentCount = -54;

            // Act
            Result<long[,]> result = FileSegmentHelper.CalculateFileSegmentRanges(fileSize, segmentCount);

            // Assert => Whether the processing is failed.
            result.IsSuccess.Should().BeFalse();

            // Assert => Whether the result is correct.
            result.Value.Should().BeNull();

            // Assert => Whether the error message is correct.
            result.ErrorMessage.Should().Be("File size and segment count must be greater than zero.");
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
                var result = FileSegmentHelper.CombineSegmentsSafe(segments, ref finalFileStream);

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
            Result<bool> result = FileSegmentHelper.CombineSegmentsSafe(Array.Empty<string>(), ref finalFileStream);

            // Assert => Whether the processing is failed.
            result.IsSuccess.Should().BeFalse("because no segments were provided");
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();

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
            Result<string[]> result = FileSegmentHelper.SplitPaths(maxThreads, "UserData\\Downloads\\file.zip");

            // Assert => Whether the processing is failed.
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("cannot be 0");
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
            Result<string[]> result = FileSegmentHelper.SplitPaths(maxThreads, directoryPath);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty()
                .And.Contain("file name"); // Assuming your error message mentions that a file name is required
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
            Result<string[]> result = FileSegmentHelper.SplitPaths(maxThreads, testFilePath);

            // Assert => Whether the processing is successful.
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(maxThreads);
            for (int i = 0; i < result.Value.Length; i++)
            {
                result.Value[i].Should().Be(Path.Combine("UserData", "Downloads", $"file-{i}.downtemp"));
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
            Result<string[]> result = FileSegmentHelper.SplitPaths(maxThreads, testFilePath);

            // Assert => Whether the processing is successful.
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNullOrEmpty();
            result.Value.Should().HaveCount(2);
            result.Value[0].Should().EndWith("myfile-0.downtemp");
            result.Value[1].Should().EndWith("myfile-1.downtemp");
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
            Result<string[]> result = FileSegmentHelper.SplitPaths(maxThreads, testFilePath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNullOrEmpty();
            result.Value.Should().HaveCount(1);
            result.Value[0].Should().Be("C:\\file-0.downtemp");
        }

        [Fact]
        public void SplitPaths_ShouldWorkWithNestedUnixStylePaths()
        {
            // Arrange
            int maxThreads = 2;
            string path = "home/user/downloads/file.tar.gz";

            // Act
            Result<string[]> result = FileSegmentHelper.SplitPaths(maxThreads, path);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNullOrEmpty();
            result.Value.Should().HaveCount(2);
            // Use Path.Combine to ensure corss platform compatibility
            result.Value[0].Should().Be(Path.Combine("home", "user", "downloads", "file.tar-0.downtemp"));
            result.Value[1].Should().Be(Path.Combine("home", "user", "downloads", "file.tar-1.downtemp"));
        }

        [Fact]
        public void SplitPaths_ShouldGenerateLargeNumberOfPaths()
        {
            // Arrange
            int maxThreads = 1000;
            string path = Path.Combine("UserData", "Downloads", "bigfile.bin");

            // Act
            Result<string[]> result = FileSegmentHelper.SplitPaths(maxThreads, path);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNullOrEmpty();
            result.Value.Should().HaveCount(1000);
            for (int i = 0; i < result.Value.Length; i++)
            {
                result.Value[i].Should().EndWith($"bigfile-{i}.downtemp");
            }
        }

        #endregion SplitPaths()
    }
}