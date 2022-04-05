using System;
using System.Collections.Generic;
using EPiServer.Core;
using FakeItEasy;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests.AutomaticRedirects;

public class ContentKeyGeneratorTests
{
    private readonly ContentKeyGenerator _generator;
    private List<IContentKeyProvider> _providers;

    public ContentKeyGeneratorTests()
    {
        InitProviders();
        _generator = new ContentKeyGenerator(_providers);
    }

    [Fact]
    public void GetContentKey_returns_empty_when_link_empty()
    {
        var result = _generator.GetContentKey(ContentReference.EmptyReference);

        Assert.Equal(ContentKeyResult.Empty, result);
    }

    [Fact]
    public void GetContentKey_returns_empty_when_no_results_from_providers()
    {
        var random = new Random(DateTime.Now.Millisecond);
        var contentLink = new ContentReference(random.Next());
        A.CallTo(() => _providers[0].GetContentKey(contentLink)).Returns(ContentKeyResult.Empty);
        A.CallTo(() => _providers[1].GetContentKey(contentLink)).Returns(ContentKeyResult.Empty);

        var result = _generator.GetContentKey(contentLink);

        Assert.Equal(ContentKeyResult.Empty, result);
    }

    [Fact]
    public void GetContentKey_returns_first_key()
    {
        var random = new Random(DateTime.Now.Millisecond);
        var contentLink = new ContentReference(random.Next());
        var firstKey = new ContentKeyResult("firstKey");
        A.CallTo(() => _providers[0].GetContentKey(contentLink)).Returns(firstKey);
        A.CallTo(() => _providers[1].GetContentKey(contentLink)).Returns(new ContentKeyResult(Guid.NewGuid().ToString()));

        var result = _generator.GetContentKey(contentLink);

        var expected = firstKey.Key;
        Assert.Equal(expected, result.Key);
    }

    [Fact]
    public void GetContentKey_returns_first_non_empty_key()
    {
        var random = new Random(DateTime.Now.Millisecond);
        var contentLink = new ContentReference(random.Next());
        var key = new ContentKeyResult("key");
        A.CallTo(() => _providers[0].GetContentKey(contentLink)).Returns(ContentKeyResult.Empty);
        A.CallTo(() => _providers[1].GetContentKey(contentLink)).Returns(key);

        var result = _generator.GetContentKey(contentLink);

        var expected = key.Key;
        Assert.Equal(expected, result.Key);

    }

    private void InitProviders()
    {
        _providers = new List<IContentKeyProvider> { A.Fake<IContentKeyProvider>(), A.Fake<IContentKeyProvider>() };
        A.CallTo(() => _providers[0].GetContentKey(A<ContentReference>._)).Returns(ContentKeyResult.Empty);
        A.CallTo(() => _providers[1].GetContentKey(A<ContentReference>._)).Returns(new ContentKeyResult("key"));
    }
}
