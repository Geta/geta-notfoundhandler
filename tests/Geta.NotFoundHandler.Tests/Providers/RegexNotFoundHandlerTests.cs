using Geta.NotFoundHandler.Providers;
using Geta.NotFoundHandler.Providers.RegexRedirects;
using Xunit;

namespace Geta.NotFoundHandler.Tests.Providers;

public class RegexNotFoundHandlerTests
{
    private readonly RegexRedirectNotFoundHandler _sut;

    public RegexNotFoundHandlerTests()
    {
        _sut = new RegexRedirectNotFoundHandler();
    }

    [Fact]
    public void RewriteUrl_regex_matches_url()
    {
        var result = _sut.RewriteUrl("https://test.example.com/I-123?a=b");


    }
}
