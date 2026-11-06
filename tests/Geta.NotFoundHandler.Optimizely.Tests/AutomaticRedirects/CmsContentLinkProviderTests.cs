using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Applications;
using EPiServer.Core;
using FakeItEasy;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests.AutomaticRedirects;

public class CmsContentLinkProviderTests
{
    private readonly CmsContentLinkProvider _provider;
    private readonly IApplicationRepository _fakeApplicationRepository;
    private readonly IContentLoader _fakeContentLoader;

    public CmsContentLinkProviderTests()
    {
        _fakeApplicationRepository = A.Fake<IApplicationRepository>();
        _fakeContentLoader = A.Fake<IContentLoader>();
        _provider = new CmsContentLinkProvider(_fakeApplicationRepository, _fakeContentLoader);
    }

    [Fact]
    public void GetAllLinks_returns_descendants_for_each_site()
    {
        var numberOfSites = new Random(DateTime.Now.Millisecond).Next(1, 10);
        var apps = CreateFakeRoutableApplications(numberOfSites);
        A.CallTo(() => _fakeApplicationRepository.List()).Returns(apps);

        var _ = _provider.GetAllLinks().ToList();

        A.CallTo(() => _fakeContentLoader.GetDescendents(A<ContentReference>._)).MustHaveHappened(numberOfSites, Times.Exactly);
    }

    [Fact]
    public void GetAllLinks_returns_descendants()
    {
        var apps = CreateFakeRoutableApplications(1);
        var numberOfDescendants = new Random(DateTime.Now.Millisecond).Next(1, 10);
        var descendants = A.CollectionOfDummy<ContentReference>(numberOfDescendants);
        A.CallTo(() => _fakeApplicationRepository.List()).Returns(apps);
        A.CallTo(() => _fakeContentLoader.GetDescendents(A<ContentReference>._)).Returns(descendants);

        var result = _provider.GetAllLinks().ToList();

        Assert.Equal(numberOfDescendants, result.Count);
        Assert.Equal(descendants, result);
    }

    /// <summary>
    /// Creates fake Application instances that also implement IRoutableApplication,
    /// matching the CMS 13 application model (ISiteDefinitionRepository was replaced
    /// by IApplicationRepository in CMS 13).
    /// </summary>
    private static IEnumerable<Application> CreateFakeRoutableApplications(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => A.Fake<Application>(x => x
                .Implements<IRoutableApplication>()
                .WithArgumentsForConstructor(new object[] { $"app-{i}" })))
            .ToList();
    }
}
