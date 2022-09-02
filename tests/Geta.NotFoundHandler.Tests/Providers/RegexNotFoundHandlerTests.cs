using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FakeItEasy;
using Geta.NotFoundHandler.Core.Providers.RegexRedirects;
using Geta.NotFoundHandler.Data;
using Microsoft.Extensions.Logging;
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
        _sut = new RegexRedirectNotFoundHandler(_fakeRegexRedirectLoader, fakeLogger);
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

    private static RegexRedirect RegexRedirect(string oldUrlRegex, string newUrlFormat)
    {
        return new RegexRedirect(Guid.NewGuid(),
                                 new Regex(oldUrlRegex, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
                                 newUrlFormat,
                                 1,
                                 0);
    }
}
