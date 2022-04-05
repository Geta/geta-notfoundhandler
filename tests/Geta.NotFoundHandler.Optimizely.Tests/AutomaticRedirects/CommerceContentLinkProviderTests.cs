using System;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using FakeItEasy;
using Geta.NotFoundHandler.Optimizely.Commerce.AutomaticRedirects;
using Mediachase.Commerce.Catalog;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests.AutomaticRedirects;

public class CommerceContentLinkProviderTests
{
    private readonly ReferenceConverter _fakeReferenceConverter;
    private readonly IContentLoader _fakeContentLoader;
    private readonly CommerceContentLinkProvider _provider;

    public CommerceContentLinkProviderTests()
    {
        _fakeReferenceConverter = A.Fake<ReferenceConverter>();
        _fakeContentLoader = A.Fake<IContentLoader>();
        _provider = new CommerceContentLinkProvider(_fakeReferenceConverter, _fakeContentLoader);
    }

    [Fact]
    public void GetAllLinks_returns_root_descendents()
    {
        var random = new Random(DateTime.Now.Millisecond);
        var links = A.CollectionOfDummy<ContentReference>(random.Next(10, 100));
        var rootLink = new ContentReference(random.Next());
        A.CallTo(() => _fakeReferenceConverter.GetRootLink()).Returns(rootLink);
        A.CallTo(() => _fakeContentLoader.GetDescendents(rootLink)).Returns(links);

        var result = _provider.GetAllLinks();

        var expected = links.Count;
        Assert.Equal(expected, result.Count());
    }
}
