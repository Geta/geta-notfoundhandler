using Geta.NotFoundHandler.Infrastructure.Configuration;
using Xunit;

namespace Geta.NotFoundHandler.Tests
{
    public class NotFoundHandlerOptionsTests
    {
        [Theory]
        [InlineData("/image.jpg")]
        [InlineData("/image.jpg?v=1")]
        [InlineData("/image.jpg#top")]
        [InlineData("/image.jpg?file=document.pdf")]
        [InlineData("https://example.com/image.jpg")]
        public void IsIgnoredResource_returns_true_for_an_ignored_extension(string url)
        {
            Assert.True(new NotFoundHandlerOptions().IsIgnoredResource(url));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("/page")]
        [InlineData("/document.pdf")]
        [InlineData("/image.jpg/page")]
        [InlineData("/page?file=image.jpg")]
        [InlineData("/page#image.jpg")]
        public void IsIgnoredResource_returns_false_for_other_urls(string url)
        {
            Assert.False(new NotFoundHandlerOptions().IsIgnoredResource(url));
        }
    }
}
