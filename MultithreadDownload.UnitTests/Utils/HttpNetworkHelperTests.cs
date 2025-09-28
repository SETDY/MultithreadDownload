using FluentAssertions;
using System.Net;
using System.Text;
using MultithreadDownload.UnitTests.Fixtures;
using MultithreadDownload.Core.Errors;

namespace MultithreadDownload.UnitTests.Utils
{
    public class HttpNetworkHelperTests
    {
        #region IsVaildHttpLink()

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("ftp://example.com", false)]
        [InlineData("http:/example.com", false)]
        [InlineData("http://example.com", true)]
        [InlineData("https://example.com", true)]
        public void IsVaildHttpLink_ShouldReturnCorrectResult(string link, bool expected)
        {
            // Act => Call the method to check the link
            bool result = HttpNetworkHelper.IsVaildHttpLink(link);

            // Assert => Check if the result is successful and the value is as expected
            result.Should().Be(expected);
        }

        #endregion IsVaildHttpLink()

        #region GetWebStatusCodeAsync()

        [Fact]
        public async Task GetWebStatusCodeAsync_ShouldReturn200_ForValidLink()
        {
            // Arrange => Create a test server that returns a 200 status code
            using var server = new TestHttpServer("http://localhost:5001/test/", async (req, res) =>
            {
                res.StatusCode = 200;
                res.ContentLength64 = 5;
                if (req.HttpMethod != "HEAD")
                {
                    await res.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Hello"));
                }
            });

            // Act => Call the method to get the status code
            HttpStatusCode statusCode = await HttpNetworkHelper.GetWebStatusCodeAsync("http://localhost:5001/test/");

            // Assert => Check if the status code is 200
            statusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetWebStatusCodeAsync_ShouldReturn404_ForNotFound()
        {
            // Arrange => Create a test server that returns a 404 status code
            using var server = new TestHttpServer("http://localhost:5002/notfound/", async (req, res) =>
            {
                res.StatusCode = 404;
            });

            // Act => Call the method to get the status code
            HttpStatusCode statusCode = await HttpNetworkHelper.GetWebStatusCodeAsync("http://localhost:5002/notfound/");

            // Assert => Check if the status code is 404
            statusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion GetWebStatusCodeAsync()

        #region LinkCanConnectionAsync()

        [Fact]
        public async Task LinkCanConnectionAsync_ShouldReturnTrue_WhenAllConditionsPass()
        {
            // Arrange => Create a test server that returns a 200 status code
            using var server = new TestHttpServer("http://localhost:5003/ok/", async (req, res) =>
            {
                res.StatusCode = 200;
            });

            // Act => Call the method to check the link
            bool result = await HttpNetworkHelper.LinkCanConnectionAsync("http://localhost:5003/ok/");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task LinkCanConnectionAsync_ShouldReturnFalse_WhenInvalidLink()
        {
            // Act => Call the method to check the link
            bool result = await HttpNetworkHelper.LinkCanConnectionAsync("ftp://invalid-link");

            // Assert => Check if the result is false
            result.Should().BeFalse();
        }

        [Fact]
        public async Task LinkCanConnectionAsync_ShouldReturnFalse_When404()
        {
            // Arrange => Create a test server that returns a 404 status code
            using var server = new TestHttpServer("http://localhost:5004/notfound/", async (req, res) =>
            {
                res.StatusCode = 404;
            });

            // Act => Call the method to check the link
            bool result = await HttpNetworkHelper.LinkCanConnectionAsync("http://localhost:5004/notfound/");

            // Assert => Check if the result is false
            result.Should().BeFalse();
        }

        #endregion LinkCanConnectionAsync()

        #region GetLinkFileSizeAsync()

        [Fact]
        public async Task GetLinkFileSizeAsync_ShouldReturnCorrectSize()
        {
            // Arrange => Create a test server that returns a known file size
            const string body = "Hello World!";
            using var server = new TestHttpServer("http://localhost:5005/file/", async (req, res) =>
            {
                res.StatusCode = 200;
                byte[] content = Encoding.UTF8.GetBytes(body);
                res.ContentLength64 = content.Length;
                if (req.HttpMethod != "HEAD")
                {
                    await res.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Hello"));
                }
            });

            // Act => Call the method to get the file size
            Result<long, DownloadError> result = await HttpNetworkHelper.GetLinkFileSizeAsync("http://localhost:5005/file/");

            // Assert => Check if the result is successful and the size is correct
            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be(12); // "Hello World!" length
        }

        [Fact]
        public async Task GetLinkFileSizeAsync_ShouldReturnFailure_WhenNoContentLength()
        {
            // Arrange => Create a test server that does not set ContentLength64
            using var server = new TestHttpServer("http://localhost:5006/nosize/", async (req, res) =>
            {
                res.StatusCode = 200;
                if (req.HttpMethod != "HEAD")
                {
                    await res.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Hello"));
                }
            });

            // Act => Call the method to get the file size
            var result = await HttpNetworkHelper.GetLinkFileSizeAsync("http://localhost:5006/nosize/");

            // Assert => Check if the result is successful and the size is 0
            // Note: ContentLength64 = 0 when not set explicitly
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task GetLinkFileSizeAsync_ShouldReturnFailure_WhenLinkIsNull()
        {
            // Act => Call the method with a null link
            Result<long, DownloadError> result = await HttpNetworkHelper.GetLinkFileSizeAsync(null);

            // Assert => Check if the result is a failure
            result.IsSuccess.Should().BeFalse();
        }

        #endregion GetLinkFileSizeAsync()
    }
}