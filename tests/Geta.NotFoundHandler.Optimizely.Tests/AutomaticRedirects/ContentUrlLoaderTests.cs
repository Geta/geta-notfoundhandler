using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Web.Routing;
using FakeItEasy;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests.AutomaticRedirects;

public class ContentUrlLoaderTests
{
    private readonly Random _random = new Random(DateTime.Now.Millisecond);
    private readonly IList<IContentUrlProvider> _fakeContentUrlProviders;
    private readonly IContentVersionRepository _fakeContentVersionRepository;
    private readonly IContentLoader _fakeContentLoader;
    private readonly IUrlResolver _fakeUrlResolver;
    private readonly ContentUrlLoader _loader;

    public ContentUrlLoaderTests()
    {
        _fakeContentUrlProviders = new List<IContentUrlProvider> { A.Fake<IContentUrlProvider>(), A.Fake<IContentUrlProvider>() };
        _fakeContentVersionRepository = A.Fake<IContentVersionRepository>();
        _fakeContentLoader = A.Fake<IContentLoader>();
        _fakeUrlResolver = A.Fake<IUrlResolver>();
        _loader = new ContentUrlLoader(_fakeContentUrlProviders,
                                       _fakeContentVersionRepository,
                                       _fakeContentLoader,
                                       _fakeUrlResolver);

        InitFakes();
    }

    [Fact]
    public void GetUrls_returns_no_urls_when_no_published_version()
    {
        var contentLink = CreateContentLink();
        A.CallTo(() => _fakeContentVersionRepository.LoadPublished(contentLink)).Returns(null);

        var result = _loader.GetUrls(contentLink);

        Assert.Empty(result);
    }

    [Fact]
    public void GetUrls_returns_urls_filtering_out_empty()
    {
        var contentLink = CreateContentLink();
        var emptyUrls = CreateTypedUrls(() => string.Empty);
        var urls = CreateTypedUrls(() => Guid.NewGuid().ToString());
        A.CallTo(() => _fakeContentUrlProviders[0].GetUrls(A<IContent>._)).Returns(urls.Union(emptyUrls));

        var result = _loader.GetUrls(contentLink);

        Assert.Equal(urls, result);
    }

    [Fact]
    public void GetUrls_returns_urls_with_fallback_when_not_handled()
    {
        var contentLink = CreateContentLink();
        var fallbackUrl = "/fallback-url";
        A.CallTo(() => _fakeContentUrlProviders[0].CanHandle(A<IContent>._)).Returns(false);
        A.CallTo(() => _fakeContentUrlProviders[1].CanHandle(A<IContent>._)).Returns(false);
        A.CallTo(() => _fakeUrlResolver.GetUrl(A<ContentReference>._, A<string>._, A<UrlResolverArguments>._))
            .Returns(fallbackUrl);

        var result = _loader.GetUrls(contentLink);

        Assert.Contains(result, x => x.Url == fallbackUrl);
    }

    [Theory]
    [InlineData("https://example.com/relative-url", "/relative-url")]
    [InlineData("//single-slash-url", "/single-slash-url")]
    public void GetUrls_returns_normalized_urls(string resolvedUrl, string expectedUrl)
    {
        var contentLink = CreateContentLink();
        var urls = new List<TypedUrl> { new() { Type = UrlType.Primary, Url = resolvedUrl } };
        A.CallTo(() => _fakeContentUrlProviders[0].GetUrls(A<IContent>._)).Returns(urls);

        var result = _loader.GetUrls(contentLink);

        Assert.Contains(result, x => x.Url == expectedUrl);
    }

    private IList<TypedUrl> CreateTypedUrls(Func<string> urlFactory)
    {
        var emptyUrls = A.CollectionOfDummy<TypedUrl>(_random.Next(2, 10));
        foreach (var emptyUrl in emptyUrls)
        {
            emptyUrl.Url = urlFactory();
        }

        return emptyUrls;
    }

    private void InitFakes()
    {
        var contentLink = CreateContentLink();
        var contentVersion = new ContentVersion(contentLink,
                                                "Name",
                                                VersionStatus.Published,
                                                DateTime.UtcNow,
                                                "SavedBy",
                                                "ChangedBy",
                                                _random.Next(),
                                                "en",
                                                true,
                                                false);
        var content = A.Dummy<IContent>();
        content.ContentLink = contentLink;
        A.CallTo(() => _fakeContentVersionRepository.LoadPublished(A<ContentReference>._)).Returns(contentVersion);
        A.CallTo(() => _fakeContentLoader.Get<IContent>(contentLink)).Returns(content);
        A.CallTo(() => _fakeContentUrlProviders[1].CanHandle(content)).Returns(true);
        A.CallTo(() => _fakeUrlResolver.GetUrl(content.ContentLink, A<string>._, A<UrlResolverArguments>._))
            .Returns(Guid.NewGuid().ToString());
    }

    private ContentReference CreateContentLink()
    {
        return new ContentReference(_random.Next());
    }
}
