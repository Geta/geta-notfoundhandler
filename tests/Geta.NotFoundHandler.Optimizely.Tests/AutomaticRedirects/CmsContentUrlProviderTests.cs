using System;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Web.Routing;
using FakeItEasy;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests.AutomaticRedirects;

public class CmsContentUrlProviderTests
{
    private readonly CmsContentUrlProvider _provider;
    private readonly IContentLoader _fakeContentLoader;
    private readonly IContentVersionRepository _fakeContentVersionRepository;
    private readonly IUrlResolver _fakeUrlResolver;

    public CmsContentUrlProviderTests()
    {
        _fakeContentLoader = A.Fake<IContentLoader>();
        _fakeContentVersionRepository = A.Fake<IContentVersionRepository>();
        _fakeUrlResolver = A.Fake<IUrlResolver>();
        _provider = new CmsContentUrlProvider(_fakeContentLoader, _fakeContentVersionRepository, _fakeUrlResolver);
    }

    [Fact]
    public void GetUrls_returns_empty_list_when_cant_handle_type()
    {
        var dummyContent = A.Dummy<IContent>();

        var results = _provider.GetUrls(dummyContent);

        Assert.Empty(results);
    }

    [Fact]
    public void GetUrls_returns_empty_list_when_page_publishing_expired()
    {
        var page = A.Dummy<PageData>();
        page.StopPublish = DateTime.UtcNow.AddDays(-1);

        var results = _provider.GetUrls(page);

        Assert.Empty(results);
    }

    [Fact]
    public void GetUrls_returns_page_url()
    {
        var page = CreatePage();
        var url = "/test-page";
        A.CallTo(() => _fakeUrlResolver.GetUrl(page.ContentLink, A<string>._, A<UrlResolverArguments>._)).Returns(url);

        var results = _provider.GetUrls(page).ToList();

        Assert.Single(results);
        Assert.Collection(results,
                          x =>
                          {
                              Assert.Equal(url, x.Url);
                              Assert.Equal(UrlType.Primary, x.Type);
                          });
    }

    [Theory]
    [InlineData(PageShortcutType.External)]
    [InlineData(PageShortcutType.Shortcut)]
    public void GetUrls_returns_url_built_from_parent_when_page_with_redirect(PageShortcutType linkType)
    {
        var page = CreatePageWithLinkType(linkType);
        page.URLSegment = "test-page";
        var parentPage = CreatePage();
        var parentUrl = "/parent-page";
        A.CallTo(() => _fakeUrlResolver.GetUrl(parentPage.ContentLink, A<string>._, A<UrlResolverArguments>._))
            .Returns(parentUrl);
        MockParentLoading(page.ParentLink, parentPage);

        var results = _provider.GetUrls(page).ToList();

        var expectedUrl = $"{parentUrl}/{page.URLSegment}/";
        Assert.Single(results);
        Assert.Collection(results,
                          x =>
                          {
                              Assert.Equal(expectedUrl, x.Url);
                              Assert.Equal(UrlType.Primary, x.Type);
                          });
    }

    [Fact]
    public void GetUrls_returns_url_to_root_when_page_with_redirect_and_parent_is_not_page()
    {
        var page = CreatePageWithLinkType(PageShortcutType.External);
        page.URLSegment = "test-page";
        var parent = A.Dummy<IContent>();
        MockParentLoading(page.ParentLink, parent);

        var results = _provider.GetUrls(page).ToList();

        var expectedUrl = "/";
        Assert.Single(results);
        Assert.Collection(results,
                          x =>
                          {
                              Assert.Equal(expectedUrl, x.Url);
                              Assert.Equal(UrlType.Primary, x.Type);
                          });
    }

    private void MockParentLoading(ContentReference parentLink, IContent parent)
    {
        var contentVersion = A.Dummy<ContentVersion>();
        A.CallTo(() => _fakeContentVersionRepository.LoadPublished(parentLink)).Returns(contentVersion);
        A.CallTo(() => _fakeContentLoader.Get<IContent>(contentVersion.ContentLink)).Returns(parent);
    }

    private static PageData CreatePage()
    {
        return CreatePageWithLinkType(PageShortcutType.Inactive);
    }

    private static PageData CreatePageWithLinkType(PageShortcutType linkType)
    {
        var page = A.Dummy<PageData>();
        page.StopPublish = null;
        page.LinkType = linkType;
        return page;
    }
}
