using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FakeItEasy;
using Geta.NotFoundHandler.Core.Providers.RegexRedirects;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Geta.NotFoundHandler.Tests.Providers;

public class RegexNotFoundHandlerTests
{
    private readonly IRegexRedirectLoader _fakeRegexRedirectLoader;
    private readonly RegexRedirectNotFoundHandler _sut;

    public RegexNotFoundHandlerTests()
    {
        _fakeRegexRedirectLoader = A.Fake<IRegexRedirectLoader>();
        var fakeLogger = A.Fake<ILogger<RegexRedirectNotFoundHandler>>();
        var fakeOptions = A.Fake<IOptions<NotFoundHandlerOptions>>();
        _sut = new RegexRedirectNotFoundHandler(_fakeRegexRedirectLoader, fakeLogger, fakeOptions);
    }

    [Theory]
    [InlineData("/$1", "https://test.example.com/x//evil.com", "/evil.com")]
    [InlineData("/$1", @"https://test.example.com/x/\evil.com", "/evil.com")]
    [InlineData("$1", "https://test.example.com/x///evil.com", "/evil.com")]
    [InlineData("/$1", "https://test.example.com/x/page", "/page")]
    [InlineData("//cdn.example.com/$1", "https://test.example.com/x/page", "//cdn.example.com/page")]
    [InlineData("https://other.example.com/$1", "https://test.example.com/x//page", "https://other.example.com//page")]
    public void RewriteUrl_does_not_let_the_request_choose_the_host(string format, string url, string expected)
    {
        A.CallTo(() => _fakeRegexRedirectLoader.GetAll())
            .Returns(new List<RegexRedirect> { RegexRedirect(@"^https?://[^/]+/x/(.*)$", format) });

        var result = _sut.RewriteUrl(url);

        Assert.Equal(expected, result.NewUrl);
    }

    [Fact]
    public void RewriteUrl_regex_matches_url_with_named_groups()
    {
        var regexRedirects = new List<RegexRedirect>
        {
            RegexRedirect(@"(?<code>I-[^=?]+)[?]{0,1}(?<query>.*)", "/catalog-content/redirect-by-code?code=${code}&${query}")
        };
        A.CallTo(() => _fakeRegexRedirectLoader.GetAll()).Returns(regexRedirects);

        var result = _sut.RewriteUrl("https://test.example.com/I-123?a=b");

        Assert.Equal("/catalog-content/redirect-by-code?code=I-123&a=b", result.NewUrl);
    }

    [Fact]
    public void RewriteUrl_regex_matches_url_with_group_index()
    {
        var regexRedirects = new List<RegexRedirect>
        {
            RegexRedirect(@"(I-[^=?]+)[?]{0,1}(.*)", "/catalog-content/redirect-by-code?code=$1&$2")
        };
        A.CallTo(() => _fakeRegexRedirectLoader.GetAll()).Returns(regexRedirects);

        var result = _sut.RewriteUrl("https://test.example.com/I-123?a=b");

        Assert.Equal("/catalog-content/redirect-by-code?code=I-123&a=b", result.NewUrl);
    }

    private static RegexRedirect RegexRedirect(string oldUrlRegex, string newUrlFormat, int orderNumber = 1, int timoutCount = 0)
    {
        return new RegexRedirect(Guid.NewGuid(),
                                 new Regex(oldUrlRegex, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
                                 newUrlFormat,
                                 orderNumber,
                                 timoutCount,
                                 DateTime.UtcNow,
                                 DateTime.UtcNow);
    }
}
