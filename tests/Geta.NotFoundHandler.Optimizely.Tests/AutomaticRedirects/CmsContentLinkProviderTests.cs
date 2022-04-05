using System;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Web;
using FakeItEasy;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests.AutomaticRedirects;

public class CmsContentLinkProviderTests
{
    private readonly CmsContentLinkProvider _provider;
    private readonly ISiteDefinitionRepository _fakeSiteDefinitionRepository;
    private readonly IContentLoader _fakeContentLoader;

    public CmsContentLinkProviderTests()
    {
        _fakeSiteDefinitionRepository = A.Fake<ISiteDefinitionRepository>();
        _fakeContentLoader = A.Fake<IContentLoader>();
        _provider = new CmsContentLinkProvider(_fakeSiteDefinitionRepository, _fakeContentLoader);
    }

    [Fact]
    public void GetAllLinks_returns_descendants_for_each_site()
    {
        var numberOfSites = new Random(DateTime.Now.Millisecond).Next(1, 10);
        var sites = A.CollectionOfDummy<SiteDefinition>(numberOfSites);
        A.CallTo(() => _fakeSiteDefinitionRepository.List()).Returns(sites);

        var _ = _provider.GetAllLinks().ToList();

        A.CallTo(() => _fakeContentLoader.GetDescendents(A<ContentReference>._)).MustHaveHappened(numberOfSites, Times.Exactly);
    }

    [Fact]
    public void GetAllLinks_returns_descendants()
    {
        var sites = A.CollectionOfDummy<SiteDefinition>(1);
        var numberOfDescendants = new Random(DateTime.Now.Millisecond).Next(1, 10);
        var descendants = A.CollectionOfDummy<ContentReference>(numberOfDescendants);
        A.CallTo(() => _fakeSiteDefinitionRepository.List()).Returns(sites);
        A.CallTo(() => _fakeContentLoader.GetDescendents(A<ContentReference>._)).Returns(descendants);

        var result = _provider.GetAllLinks().ToList();

        Assert.Equal(numberOfDescendants, result.Count);
        Assert.Equal(descendants, result);
    }
}
