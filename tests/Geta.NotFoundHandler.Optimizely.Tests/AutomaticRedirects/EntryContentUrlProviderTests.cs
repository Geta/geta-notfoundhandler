using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Core;
using EPiServer.Web.Routing;
using FakeItEasy;
using Geta.NotFoundHandler.Optimizely.Commerce.AutomaticRedirects;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests.AutomaticRedirects;

public class EntryContentUrlProviderTests
{
    private readonly EntryContentUrlProvider _provider;
    private readonly IUrlResolver _fakeUrlResolver;
    private readonly IRelationRepository _fakeRelationRepository;

    public EntryContentUrlProviderTests()
    {
        _fakeUrlResolver = A.Fake<IUrlResolver>();
        _fakeRelationRepository = A.Fake<IRelationRepository>();
        _provider = new EntryContentUrlProvider(_fakeUrlResolver, _fakeRelationRepository);
    }

    [Fact]
    public void GetUrls_returns_empty_list_when_cant_handle_type()
    {
        var dummyContent = A.Dummy<IContent>();

        var results = _provider.GetUrls(dummyContent);

        Assert.Empty(results);
    }

    [Fact]
    public void GetUrls_returns_seo_url()
    {
        var entry = A.Dummy<EntryContentBase>();

        var results = _provider.GetUrls(entry);

        var expected = new TypedUrl { Url = $"/{entry.SeoUri}", Type = UrlType.Seo };
        Assert.Contains(results, url => url == expected);
    }

    [Fact]
    public void GetUrls_returns_primary_url()
    {
        var entry = A.Dummy<EntryContentBase>();
        var parentsLinks = A.CollectionOfDummy<NodeEntryRelation>(new Random(DateTime.Now.Millisecond).Next(1, 10));
        var primaryLink = parentsLinks.First();
        primaryLink.IsPrimary = true;
        var primaryUrl = "/primary-url";
        A.CallTo(() => _fakeRelationRepository.GetParents<NodeEntryRelation>(entry.ContentLink)).Returns(parentsLinks);
        A.CallTo(() => _fakeUrlResolver.GetUrl(primaryLink.Parent, A<string>._, A<UrlResolverArguments>._)).Returns(primaryUrl);

        var results = _provider.GetUrls(entry);

        var expected = new TypedUrl { Url = $"{primaryUrl}/{entry.RouteSegment}", Type = UrlType.Primary };
        Assert.Contains(results, url => url == expected);
    }

    [Fact]
    public void GetUrls_returns_secondary_urls()
    {
        var entry = A.Dummy<EntryContentBase>();
        var parentsLinks = A.CollectionOfDummy<NodeEntryRelation>(new Random(DateTime.Now.Millisecond).Next(2, 10));
        var primaryLink = parentsLinks.First();
        primaryLink.IsPrimary = true;
        parentsLinks = ParentLinksWithSecondaryLinks(parentsLinks, primaryLink);
        A.CallTo(() => _fakeRelationRepository.GetParents<NodeEntryRelation>(entry.ContentLink)).Returns(parentsLinks);

        var results = _provider.GetUrls(entry).ToList();

        var secondaryLinkCount = results.Count(x => x.Type == UrlType.Secondary);
        var expectedCount = parentsLinks.Count - 1;
        Assert.Equal(expectedCount, secondaryLinkCount);
    }

    private static IList<NodeEntryRelation> ParentLinksWithSecondaryLinks(
        IList<NodeEntryRelation> parentsLinks,
        NodeEntryRelation primaryLink)
    {
        var secondaryLinks = parentsLinks.Skip(1).ToList();
        foreach (var secondaryLink in secondaryLinks)
        {
            secondaryLink.IsPrimary = false;
        }

        return new[] { primaryLink }.Union(secondaryLinks).ToList();
    }
}
