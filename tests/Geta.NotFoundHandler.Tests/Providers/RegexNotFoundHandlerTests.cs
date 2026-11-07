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
    [InlineData("$1", "https://test.example.com/x/https://evil.com/a", "/https://evil.com/a")]
    [InlineData("$1", "https://test.example.com/x/https:evil.com", "/https:evil.com")]
    [InlineData("$1", "https://test.example.com/x/https://test.example.com@evil.com", "/https://test.example.com@evil.com")]
    [InlineData("$1", "https://test.example.com/x/https://test.example.com/a", "https://test.example.com/a")]
    public void RewriteUrl_does_not_let_the_request_choose_the_host(string format, string url, string expected)
    {
        A.CallTo(() => _fakeRegexRedirectLoader.GetAll())
            .Returns(new List<RegexRedirect> { RegexRedirect(@"^https?://[^/]+/x/(.*)$", format) });

        var result = _sut.RewriteUrl(url);

        Assert.Equal(expected, result.NewUrl);
    }

    [Fact]
    public void RewriteUrl_keeps_a_host_named_in_the_format_when_the_scheme_is_captured()
    {
        A.CallTo(() => _fakeRegexRedirectLoader.GetAll())
            .Returns(new List<RegexRedirect> { RegexRedirect(@"^(https?)://old\.example\.com/(.*)$", "$1://new.example.com/$2") });

        var result = _sut.RewriteUrl("https://old.example.com/page");

        Assert.Equal("https://new.example.com/page", result.NewUrl);
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

    /// <summary>
    /// https://github.com/Geta/geta-notfoundhandler/issues/178
    /// </summary>
    [Theory]
    [InlineData(@"^/en/blog/(.*)", "/en/resources/blog/$1", "https://test.example.com/en/blog/article", "/en/resources/blog/article")]
    [InlineData(@"^/en/blog/(.*)$", "/new/$1", "https://test.example.com/en/blog/article?a=b", "/new/article?a=b")]
    [InlineData(@"^https://test\.example\.com/en/(.*)", "/new/$1", "https://test.example.com/en/article", "/new/article")]
    [InlineData(@"/en/(.*)", "/new/$1", "https://test.example.com/en/article", "/new/article")]
    [InlineData(@"^\/en\/(.*)", "/new/$1", "https://test.example.com/en/article", "/new/article")]
    [InlineData(@"^(/en/)(.+)$", "/new/$2", "https://test.example.com/en/article", "/new/article")]
    [InlineData(@"^(?:/en/)(.+)$", "/new/$1", "https://test.example.com/en/article", "/new/article")]
    [InlineData(@"^(?<lang>/en/)(?<rest>.+)$", "/new/${rest}", "https://test.example.com/en/article", "/new/article")]
    [InlineData(@"(?i)^/EN/(.*)", "/new/$1", "https://test.example.com/en/article", "/new/article")]
    [InlineData(@"\A/en/(.*)", "/new/$1", "https://test.example.com/en/article", "/new/article")]
    public void RewriteUrl_matches_a_rule_written_for_the_whole_url_or_for_the_path(string pattern, string format, string url, string expected)
    {
        A.CallTo(() => _fakeRegexRedirectLoader.GetAll()).Returns(new List<RegexRedirect> { RegexRedirect(pattern, format) });

        var result = _sut.RewriteUrl(url);

        Assert.Equal(expected, result.NewUrl);
    }

    [Fact]
    public void RewriteUrl_keeps_the_order_of_the_rules_when_a_later_one_matches_the_whole_url()
    {
        A.CallTo(() => _fakeRegexRedirectLoader.GetAll()).Returns(new List<RegexRedirect>
        {
            RegexRedirect(@"^/en/(.*)", "/first/$1"),
            RegexRedirect(@"^https://test\.example\.com/en/(.*)", "/second/$1")
        });

        var result = _sut.RewriteUrl("https://test.example.com/en/article");

        Assert.Equal("/first/article", result.NewUrl);
    }

    [Fact]
    public void RewriteUrl_prefers_the_whole_url_so_a_captured_group_stays_what_it_was()
    {
        A.CallTo(() => _fakeRegexRedirectLoader.GetAll()).Returns(new List<RegexRedirect> { RegexRedirect(@"^(.*)/en/(.*)$", "/new$1") });

        var result = _sut.RewriteUrl("https://test.example.com/en/article");

        Assert.Equal("/newhttps://test.example.com", result.NewUrl);
    }

    [Theory]
    [InlineData(@"^(?!https://www\.example\.com/).*")]
    [InlineData(@"^[^:]+$")]
    public void RewriteUrl_only_gives_the_path_to_a_rule_that_could_never_match_a_whole_url(string pattern)
    {
        A.CallTo(() => _fakeRegexRedirectLoader.GetAll()).Returns(new List<RegexRedirect> { RegexRedirect(pattern, "https://www.example.com/") });

        var result = _sut.RewriteUrl("https://www.example.com/missing");

        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void RewriteUrl_does_not_match_a_path_rule_on_another_path()
    {
        A.CallTo(() => _fakeRegexRedirectLoader.GetAll()).Returns(new List<RegexRedirect> { RegexRedirect(@"^/en/(.*)", "/new/$1") });

        var result = _sut.RewriteUrl("https://test.example.com/no/en/article");

        Assert.True(result.IsEmpty);
    }

    [Theory]
    [InlineData("/$1", "https://test.example.com/x//evil.com", "/evil.com")]
    [InlineData("$1", "https://test.example.com/x///evil.com", "/evil.com")]
    public void RewriteUrl_does_not_let_a_path_rule_choose_the_host(string format, string url, string expected)
    {
        A.CallTo(() => _fakeRegexRedirectLoader.GetAll()).Returns(new List<RegexRedirect> { RegexRedirect("^/x/(.*)", format) });

        var result = _sut.RewriteUrl(url);

        Assert.Equal(expected, result.NewUrl);
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
