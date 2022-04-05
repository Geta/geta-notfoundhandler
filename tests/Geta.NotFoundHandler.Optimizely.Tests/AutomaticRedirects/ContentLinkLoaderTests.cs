using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Core;
using FakeItEasy;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests.AutomaticRedirects;

public class ContentLinkLoaderTests
{
    private readonly Random _random = new Random(DateTime.Now.Millisecond);

    [Fact]
    public void GetAllLinks_returns_links_from_all_providers()
    {
        var firstProvider = A.Fake<IContentLinkProvider>();
        var secondProvider = A.Fake<IContentLinkProvider>();
        var commonLinks = GetDummyLinks();
        var firstLinks = GetDummyLinks().Union(commonLinks).ToList();
        var secondLinks = GetDummyLinks().Union(commonLinks).ToList();
        A.CallTo(() => firstProvider.GetAllLinks()).Returns(firstLinks);
        A.CallTo(() => secondProvider.GetAllLinks()).Returns(secondLinks);
        var loader = new ContentLinkLoader(new[] { firstProvider, secondProvider });

        var links = loader.GetAllLinks();

        var expected = firstLinks.Union(secondLinks).Distinct().ToList();
        Assert.Equal(expected, links);
    }

    private IList<ContentReference> GetDummyLinks()
    {
        return A.CollectionOfDummy<ContentReference>(_random.Next(10, 50));
    }
}
