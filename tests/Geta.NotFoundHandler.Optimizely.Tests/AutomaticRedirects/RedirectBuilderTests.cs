using System;
using System.Collections.Generic;
using System.Linq;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Geta.NotFoundHandler.Optimizely.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests.AutomaticRedirects;

public class RedirectBuilderTests
{
    private readonly RedirectBuilder _redirectBuilder;
    private const string DefaultContentKey = "312";
    private const string DefaultPrimaryUrl = "/primary-default";

    public RedirectBuilderTests()
    {
        _redirectBuilder = new(
            Options(RedirectType.Permanent),
            Microsoft.Extensions.Options.Options.Create(new NotFoundHandlerOptions()));
    }

    /// <summary>
    /// https://github.com/Geta/geta-notfoundhandler/issues/154
    /// </summary>
    [Theory]
    [InlineData("/contentassets/e4f9/image.jpg")]
    [InlineData("/contentassets/e4f9/image.png?v=1")]
    [InlineData("https://example.com/globalassets/styles.css")]
    public void CreateRedirects_skips_urls_the_request_handler_never_redirects(string url)
    {
        var redirects = _redirectBuilder.CreateRedirects(new List<ContentUrlHistory>
        {
            History(DateTime.UtcNow, new TypedUrl { Url = "/new-location", Language = "en", Type = UrlType.Primary }),
            History(DateTime.UtcNow.AddDays(-1), new TypedUrl { Url = url, Language = "en", Type = UrlType.Primary })
        });

        Assert.Empty(redirects);
    }

    [Theory]
    [InlineData("/contentassets/e4f9/document.pdf")]
    [InlineData("/about.us/page")]
    [InlineData("/page")]
    public void CreateRedirects_keeps_other_urls_of_moved_content(string url)
    {
        var redirects = _redirectBuilder.CreateRedirects(new List<ContentUrlHistory>
        {
            History(DateTime.UtcNow, new TypedUrl { Url = "/new-location", Language = "en", Type = UrlType.Primary }),
            History(DateTime.UtcNow.AddDays(-1), new TypedUrl { Url = url, Language = "en", Type = UrlType.Primary })
        });

        Assert.Equal(url, Assert.Single(redirects).OldUrl);
    }

    [Fact]
    public void CreateRedirects_keeps_a_resource_url_when_the_site_no_longer_ignores_that_extension()
    {
        var builder = new RedirectBuilder(
            Options(RedirectType.Permanent),
            Microsoft.Extensions.Options.Options.Create(new NotFoundHandlerOptions { IgnoredResourceExtensions = new[] { "css" } }));

        var redirects = builder.CreateRedirects(new List<ContentUrlHistory>
        {
            History(DateTime.UtcNow, new TypedUrl { Url = "/new.jpg", Language = "en", Type = UrlType.Primary }),
            History(DateTime.UtcNow.AddDays(-1), new TypedUrl { Url = "/old.jpg", Language = "en", Type = UrlType.Primary })
        });

        Assert.Equal("/old.jpg", Assert.Single(redirects).OldUrl);
    }

    private static ContentUrlHistory History(DateTime created, params TypedUrl[] urls)
    {
        return new ContentUrlHistory { ContentKey = DefaultContentKey, CreatedUtc = created, Urls = urls.ToList() };
    }

    /// <summary>
    /// https://github.com/Geta/geta-notfoundhandler/issues/169
    /// </summary>
    [Fact]
    public void CreateRedirects_returns_nothing_when_content_has_no_history()
    {
        var redirects = _redirectBuilder.CreateRedirects(new List<ContentUrlHistory>());

        Assert.Empty(redirects);
    }

    [Fact]
    public void CreateRedirects_skips_only_the_language_without_a_primary_url()
    {
        var histories = new List<ContentUrlHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ContentKey = DefaultContentKey,
                CreatedUtc = DateTime.Parse("2022-01-01"),
                Urls = new List<TypedUrl>
                {
                    new() { Url = "/en/old", Type = UrlType.Primary, Language = "en" },
                    new() { Url = "/no/old", Type = UrlType.Primary, Language = "no" }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                ContentKey = DefaultContentKey,
                CreatedUtc = DateTime.Parse("2022-01-02"),
                Urls = new List<TypedUrl>
                {
                    new() { Url = "/en/new", Type = UrlType.Seo, Language = "en" },
                    new() { Url = "/no/new", Type = UrlType.Primary, Language = "no" }
                }
            }
        };

        var redirects = _redirectBuilder.CreateRedirects(histories).ToList();

        var redirect = Assert.Single(redirects);
        Assert.Equal("/no/old", redirect.OldUrl);
        Assert.Equal("/no/new", redirect.NewUrl);
    }

    [Fact]
    public void CreateRedirects_returns_primary_redirect()
    {
        var oldUrl = "/initial";
        var newUrl = "/destination";
        var histories = new List<ContentUrlHistory>
        {
            HistoryWithPrimaryUrl("2022-01-01", oldUrl), HistoryWithPrimaryUrl("2022-01-02", newUrl)
        };

        var redirects = _redirectBuilder.CreateRedirects(histories).ToList();

        Assert.Single(redirects);
        Assert.Contains(redirects, x => x.OldUrl == oldUrl && x.NewUrl == newUrl);
    }

    [Fact]
    public void CreateRedirects_returns_multiple_primary_redirects()
    {
        var firstUrl = "/first";
        var secondUrl = "/second";
        var newUrl = "/destination";
        var histories = new List<ContentUrlHistory>
        {
            HistoryWithPrimaryUrl("2022-01-01", firstUrl),
            HistoryWithPrimaryUrl("2022-01-02", secondUrl),
            HistoryWithPrimaryUrl("2022-01-03", newUrl)
        };

        var redirects = _redirectBuilder.CreateRedirects(histories).ToList();

        Assert.Equal(2, redirects.Count);
        Assert.Contains(redirects, x => x.OldUrl == firstUrl && x.NewUrl == newUrl);
        Assert.Contains(redirects, x => x.OldUrl == secondUrl && x.NewUrl == newUrl);
    }

    [Fact]
    public void CreateRedirects_returns_seo_redirect()
    {
        var oldUrl = "/initial";
        var newUrl = "/destination";
        var histories = new List<ContentUrlHistory>
        {
            HistoryWithSeoUrl("2022-01-01", oldUrl), HistoryWithSeoUrl("2022-01-02", newUrl)
        };

        var redirects = _redirectBuilder.CreateRedirects(histories).ToList();

        Assert.Contains(redirects, x => x.OldUrl == oldUrl && x.NewUrl == newUrl);
    }

    [Fact]
    public void CreateRedirects_returns_seo_redirect_with_fallback_to_primary()
    {
        var oldUrl = "/initial";
        var histories = new List<ContentUrlHistory>
        {
            HistoryWithSeoUrl("2022-01-01", oldUrl), HistoryWithPrimaryUrl("2022-01-02", DefaultPrimaryUrl)
        };

        var redirects = _redirectBuilder.CreateRedirects(histories).ToList();

        Assert.Contains(redirects, x => x.OldUrl == oldUrl && x.NewUrl == DefaultPrimaryUrl);
    }

    [Fact]
    public void CreateRedirects_returns_secondary_redirect_with_new_primary_url()
    {
        var firstUrl = "/first";
        var secondUrl = "/second";
        var histories = new List<ContentUrlHistory>
        {
            HistoryWithSecondaryUrl("2022-01-01", firstUrl), HistoryWithSecondaryUrl("2022-01-02", secondUrl)
        };

        var redirects = _redirectBuilder.CreateRedirects(histories).ToList();

        Assert.Contains(redirects, x => x.OldUrl == firstUrl && x.NewUrl == DefaultPrimaryUrl);
    }

    [Fact]
    public void CreateRedirects_does_not_return_secondary_redirect_when_not_changed()
    {
        var sameUrl = "/same-url";
        var histories = new List<ContentUrlHistory>
        {
            HistoryWithSecondaryUrl("2022-01-01", sameUrl), HistoryWithSecondaryUrl("2022-01-02", sameUrl)
        };

        var redirects = _redirectBuilder.CreateRedirects(histories).ToList();

        Assert.Collection(redirects, x => Assert.NotEqual(sameUrl, x.NewUrl));
    }

    [Fact]
    public void CreateRedirects_does_not_return_redirects_when_no_primary_destination()
    {
        var oldUrl = "/initial";
        var newUrl = "/destination";
        var histories = new List<ContentUrlHistory>
        {
            HistoryWithPrimaryUrl("2022-01-01", oldUrl),
            new()
            {
                Id = Guid.NewGuid(),
                ContentKey = DefaultContentKey,
                Urls = new List<TypedUrl> { new() { Url = newUrl, Type = UrlType.Secondary } },
                CreatedUtc = DateTime.Parse("2022-01-02")
            }
        };

        var redirects = _redirectBuilder.CreateRedirects(histories).ToList();

        Assert.Empty(redirects);
    }

    [Theory]
    [InlineData(RedirectType.Permanent)]
    [InlineData(RedirectType.Temporary)]
    public void CreateRedirect_returns_redirect_with_redirect_type(RedirectType redirectType)
    {
        var redirectBuilder = new RedirectBuilder(Options(redirectType), Microsoft.Extensions.Options.Options.Create(new NotFoundHandlerOptions()));
        var oldUrl = "/initial";
        var newUrl = "/destination";
        var histories = new List<ContentUrlHistory>
        {
            HistoryWithPrimaryUrl("2022-01-01", oldUrl), HistoryWithPrimaryUrl("2022-01-02", newUrl)
        };

        var redirects = redirectBuilder.CreateRedirects(histories).ToList();

        Assert.All(redirects, x => Assert.Equal(redirectType, x.RedirectType));
    }

    private static OptionsWrapper<OptimizelyNotFoundHandlerOptions> Options(RedirectType redirectType)
    {
        return new OptionsWrapper<OptimizelyNotFoundHandlerOptions>(
            new OptimizelyNotFoundHandlerOptions { AutomaticRedirectType = redirectType });
    }

    private static ContentUrlHistory HistoryWithSecondaryUrl(string dateString, params string[] secondaryUrls)
    {
        return HistoryWithPrimaryAndSecondaryUrl(dateString, DefaultPrimaryUrl, secondaryUrls);
    }

    private static ContentUrlHistory HistoryWithPrimaryAndSecondaryUrl(
        string dateString,
        string primaryUrl,
        params string[] secondaryUrls)
    {
        var history = HistoryWithPrimaryUrl(dateString, primaryUrl);
        foreach (var url in secondaryUrls)
        {
            history.Urls.Add(new() { Url = url, Type = UrlType.Secondary });
        }

        return history;
    }

    private static ContentUrlHistory HistoryWithSeoUrl(string dateString, string url)
    {
        var history = HistoryWithPrimaryUrl(dateString, DefaultPrimaryUrl);
        history.Urls.Add(new() { Url = url, Type = UrlType.Seo });
        return history;
    }

    private static ContentUrlHistory HistoryWithPrimaryUrl(string dateString, string url)
    {
        return HistoryWithPrimaryUrl(DefaultContentKey, dateString, url);
    }

    private static ContentUrlHistory HistoryWithPrimaryUrl(string contentKey, string dateString, string url)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            ContentKey = contentKey,
            Urls = new List<TypedUrl> { new() { Url = url, Type = UrlType.Primary } },
            CreatedUtc = DateTime.Parse(dateString)
        };
    }
}
