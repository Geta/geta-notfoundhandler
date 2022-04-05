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

public class NodeContentUrlProviderTests
{
    private readonly NodeContentUrlProvider _provider;
    private readonly IUrlResolver _fakeUrlResolver;
    private readonly IRelationRepository _fakeRelationRepository;

    public NodeContentUrlProviderTests()
    {
        _fakeUrlResolver = A.Fake<IUrlResolver>();
        _fakeRelationRepository = A.Fake<IRelationRepository>();
        _provider = new NodeContentUrlProvider(_fakeUrlResolver, _fakeRelationRepository);
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
        var node = A.Dummy<NodeContent>();

        var results = _provider.GetUrls(node);

        var expected = new TypedUrl { Url = $"/{node.SeoUri}", Type = UrlType.Seo };
        Assert.Contains(results, url => url == expected);
    }

    [Fact]
    public void GetUrls_returns_primary_url()
    {
        var node = A.Dummy<NodeContent>();
        var parentsLinks = A.CollectionOfDummy<NodeRelation>(new Random(DateTime.Now.Millisecond).Next(1, 10));
        var primaryLink = node.ParentLink;
        var primaryUrl = "/primary-url";
        A.CallTo(() => _fakeRelationRepository.GetParents<NodeRelation>(node.ContentLink)).Returns(parentsLinks);
        A.CallTo(() => _fakeUrlResolver.GetUrl(primaryLink, A<string>._, A<UrlResolverArguments>._)).Returns(primaryUrl);

        var results = _provider.GetUrls(node);

        var expected = new TypedUrl { Url = $"{primaryUrl}/{node.RouteSegment}", Type = UrlType.Primary };
        Assert.Contains(results, url => url == expected);
    }

    [Fact]
    public void GetUrls_returns_secondary_urls()
    {
        var node = A.Dummy<NodeContent>();
        var primaryLink = node.ParentLink;
        var parentsLinks = GetUniqueParentLinks();
        A.CallTo(() => _fakeRelationRepository.GetParents<NodeRelation>(node.ContentLink)).Returns(parentsLinks);
        A.CallTo(() => _fakeUrlResolver.GetUrl(primaryLink, A<string>._, A<UrlResolverArguments>._)).Returns("/primary-url");

        var results = _provider.GetUrls(node).ToList();

        var expectedCount = parentsLinks.Count;
        var secondaryLinkCount = results.Count(x => x.Type == UrlType.Secondary);
        Assert.Equal(expectedCount, secondaryLinkCount);
    }

    private static List<NodeRelation> GetUniqueParentLinks()
    {
        var random = new Random(DateTime.Now.Millisecond);
        return A.CollectionOfDummy<NodeRelation>(new Random(DateTime.Now.Millisecond).Next(2, 10))
            .Select(x =>
            {
                x.Parent = new ContentReference(random.Next());
                return x;
            }).ToList();
    }
}
