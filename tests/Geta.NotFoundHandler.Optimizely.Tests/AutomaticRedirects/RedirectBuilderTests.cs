using System;
using System.Collections.Generic;
using System.Linq;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests.AutomaticRedirects;

public class RedirectBuilderTests
{
    private readonly RedirectBuilder _redirectBuilder = new();
    private const string DefaultContentKey = "312";
    private const string DefaultPrimaryUrl = "/primary-default";

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

        Assert.Single(redirects);
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

        Assert.Single(redirects);
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

        Assert.Single(redirects);
        Assert.Contains(redirects, x => x.OldUrl == firstUrl && x.NewUrl == DefaultPrimaryUrl);
    }

    [Fact]
    public void CreateRedirects_does_not_return_secondary_redirect_when_no_changes()
    {
        var initialUrl = "/initial";
        var destinationUrl = "/destination";
        var secondaryUrl = "/secondary-unchanged";
        var histories = new List<ContentUrlHistory>
        {
            HistoryWithPrimaryAndSecondaryUrl("2022-01-01", initialUrl, secondaryUrl),
            HistoryWithPrimaryAndSecondaryUrl("2022-01-02", destinationUrl, secondaryUrl)
        };

        var redirects = _redirectBuilder.CreateRedirects(histories).ToList();

        Assert.Single(redirects);
        Assert.DoesNotContain(redirects, x => x.OldUrl == secondaryUrl && x.NewUrl == destinationUrl);
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
