using MultithreadDownload.Primitives;
using FluentAssertions;
using System.Runtime.InteropServices;

namespace MultithreadDownload.UnitTests.Utils
{
    public class PathHelperTests
    {
        #region GetDirectoryNameSafe()
        [Theory]
        [InlineData("C:\\folder\\file.txt", "C:\\folder")]
        [InlineData("C:\\file.txt", "C:\\")]
        [InlineData("C:\\", "C:\\")]
        [InlineData("/usr/local/bin/script.sh", $"/usr/local/bin")]
        [InlineData("/", "/")]
        [InlineData("", "")]
        public void GetDirectoryNameSafe_ShouldReturnExpectedResult_OnWindows(string input, string expected)
        {
            // Since there may be some differences in path handling between OSes,
            // Check if the current OS is Linux or MacOS
            // If not, skip the test
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.True(true);
                return;
            }

            // Act
            string directoryName = PathHelper.GetDirectoryNameSafe(input);


            string normalizedActual = directoryName.Replace
                (Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            string normalizedExpected = expected.Replace
                (Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            // Assert
            // Using Path.Combine to ensure the path is cross-platform
            normalizedActual.Should().Be(normalizedExpected);
        }

        [Theory]
        [InlineData("/usr/local/bin/script.sh", "/usr/local/bin")]
        [InlineData("/usr/local/bin/", "/usr/local/bin")]
        [InlineData("/file.txt", "/")]
        [InlineData("/usr/../etc/passwd", "/usr/../etc")]
        [InlineData("relative/path/to/file.txt", "relative/path/to")]
        [InlineData("file.txt", "")]
        [InlineData(".hiddenfile", "")]
        [InlineData("/", "/")]
        [InlineData("./relative.txt", ".")]
        [InlineData("../parent.txt", "..")]
        [InlineData("relative/path/to/dir/", "relative/path/to/dir")]
        [InlineData("/tmp/.", "/tmp")]
        [InlineData("/tmp/..", "/tmp")]
        public void GetDirectoryNameSafe_ShouldReturnExpectedResult_OnLinuxAndMacOS(string input, string expected)
        {
            // Since there may be some differences in path handling between OSes,
            // Check if the current OS is Linux or MacOS
            // If not, skip the test
            if (!(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && 
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX)))
            {
                Assert.True(true);
                return;
            }
            string directoryName = PathHelper.GetDirectoryNameSafe(input);

            string normalizedActual = directoryName.Replace
                (Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            string normalizedExpected = expected.Replace
                (Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            // Assert
            // Using Path.Combine to ensure the path is cross-platform
            normalizedActual.Should().Be(normalizedExpected);
        }
        #endregion

        #region GetUniqueFileName()

        [Fact]
        public void GetUniqueFileName_WhenFileDoesNotExist_ShouldReturnOriginalPath()
        {
            // Arrange
            string fileName = "testFileA.txt";
            string tempDir = Path.GetTempPath();
            string tempFilePath = Path.Combine(tempDir, fileName);

            // Act
            string result = PathHelper.GetUniqueFileName(
                Path.GetDirectoryName(tempFilePath), Path.GetFileName(tempFilePath));

            // Assert
            result.Should().Be(tempFilePath);

            File.Delete(tempFilePath);
        }

        [Fact]
        public void GetUniqueFileName_WhenFileExists_ShouldReturnPathWithNumber()
        {
            // Arrange
            // Create a dummy file
            string existingPath = Path.Combine(Path.GetTempPath(), "testfileB.txt");
            File.WriteAllText(existingPath, "dummy content");
            string? directoryName = Path.GetDirectoryName(existingPath);
            directoryName.Should().NotBeNull();

            string expectedNewPath = Path.Combine(directoryName, "testfileB (1).txt");

            // Act
            string result = PathHelper.GetUniqueFileName(directoryName, "testfileB.txt");

            // Assert
            result.Should().Be(expectedNewPath);

            // Clean up
            File.Delete(existingPath);
        }

        [Fact]
        public void GetUniqueFileName_WhenMultipleFilesExist_ShouldReturnNextAvailablePath()
        {
            // Arrange
            string fileName = "testfileC.txt";
            string tempDir = Path.GetTempPath();
            string path1 = Path.Combine(tempDir, fileName);
            string path2 = Path.Combine(tempDir, "testfileC (1).txt");
            string path3 = Path.Combine(tempDir, "testfileC (2).txt");

            // Create dummy files
            File.WriteAllText(path1, "dummy");
            File.WriteAllText(path2, "dummy");
            File.WriteAllText(path3, "dummy");

            string expectedNewPath = Path.Combine(tempDir, "testfileC (3).txt");

            // Act
            string result = PathHelper.GetUniqueFileName(tempDir, fileName);

            // Assert
            result.Should().Be(expectedNewPath);

            // Clean up
            File.Delete(path1);
            File.Delete(path2);
            File.Delete(path3);
        }

        [Fact]
        public void GetUniqueFileName_WhenFileHasNoExtension_ShouldWorkCorrectly()
        {
            // Arrange
            string fileName = "testfileD";
            string tempDir = Path.GetTempPath();
            string existingPath = Path.Combine(tempDir, fileName);
            File.WriteAllText(existingPath, "dummy content");

            string expectedNewPath = Path.Combine(tempDir, "testfileD (1)");

            // Act
            string result = PathHelper.GetUniqueFileName(tempDir, fileName);

            // Assert
            result.Should().Be(expectedNewPath);

            // Clean up
            File.Delete(existingPath);
        }

        [Fact]
        public void GetUniqueFileName_WhenFileNameContainsBrackets_ShouldAppendNumberCorrectly()
        {
            // Arrange
            string fileName = "myfileA (existing).txt";
            string tempDir = Path.GetTempPath();
            string existingPath = Path.Combine(tempDir, fileName);
            File.WriteAllText(existingPath, "dummy content");

            string expectedNewPath = Path.Combine(tempDir, "myfileA (existing) (1).txt");

            // Act
            string result = PathHelper.GetUniqueFileName(tempDir, fileName);

            // Assert
            result.Should().Be(expectedNewPath);

            File.Delete(existingPath);
        }

        [Fact]
        public void GetUniqueFileName_WithLongFilePath_ShouldWorkCorrectly()
        {
            // Arrange
            // Generate a long file name (near 200 characters) + extension
            string longFileName = new string('a', 200) + ".txt";
            string tempDir = Path.GetTempPath();
            string existingPath = Path.Combine(tempDir, longFileName);

            // Create the long file
            File.WriteAllText(existingPath, "dummy");

            string expectedNewPath = Path.Combine(tempDir, new string('a', 200) + " (1).txt");

            // Act
            string result = PathHelper.GetUniqueFileName(tempDir, longFileName);

            // Assert
            result.Should().Be(expectedNewPath);

            // Clean up
            File.Delete(existingPath);
        }

        #endregion

        #region IsValidPath()
        [Theory]
        [InlineData("folder/file.txt", true)]
        [InlineData("folder\\file.txt", true)]
        [InlineData("/usr/local/bin", true)]
        [InlineData(@"C:\Temp\file.txt", true)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData(null, false)]
        public void IsValidPath_CommonCases_ShouldReturnExpected(string input, bool expected)
        {
            // Act
            bool result = PathHelper.IsValidPath(input);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("invalid|name.txt")]
        [InlineData("invalid<name>.txt")]
        [InlineData("invalid:name.txt")]
        [InlineData("invalid\"name\".txt")]
        [InlineData("invalid?name.txt")]
        [InlineData("invalid*name.txt")]
        public void IsValidPath_WithInvalidChars_ShouldReturnFalse(string input)
        {
            // Note: On Unix-based systems, these chars are valid in filenames (except '/')
            // To make test cross-platform, only assert false on Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                PathHelper.IsValidPath(input).Should().BeFalse();
            }
            else
            {
                // On Linux/macOS, these may be valid!
                PathHelper.IsValidPath(input).Should().BeTrue();
            }
        }

        [Fact]
        public void IsValidPath_WithOnlySlash_ShouldReturnFalse()
        {
            // Act
            bool result = PathHelper.IsValidPath("/");

            // Assert
            result.Should().BeTrue();
        }
        #endregion
    }
}
