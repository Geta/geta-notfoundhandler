using System;
using EPiServer.Core;
using FakeItEasy;
using Geta.NotFoundHandler.Optimizely.Commerce.AutomaticRedirects;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Mediachase.Commerce.Catalog;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests.AutomaticRedirects;

public class CommerceContentKeyProviderTests
{
    private readonly CommerceContentKeyProvider _contentKeyProvider;
    private readonly ReferenceConverter _fakeReferenceConverter;

    private static CatalogContentType[] ValidCatalogContentTypes =
    {
        CatalogContentType.CatalogEntry, CatalogContentType.CatalogNode
    };

    public CommerceContentKeyProviderTests()
    {
        _fakeReferenceConverter = A.Fake<ReferenceConverter>();
        _contentKeyProvider = new CommerceContentKeyProvider(_fakeReferenceConverter);
        InitializeFakes();
    }

    [Fact]
    public void GetContentKey_returns_empty_when_link_is_empty()
    {
        var result = _contentKeyProvider.GetContentKey(ContentReference.EmptyReference);

        Assert.Equal(ContentKeyResult.Empty, result);
    }

    [Fact]
    public void GetContentKey_returns_empty_when_not_commerce_provider()
    {
        var nonCommerceLink = new ContentReference(778, 434, "NonCatalogContent");

        var result = _contentKeyProvider.GetContentKey(nonCommerceLink);

        Assert.Equal(ContentKeyResult.Empty, result);
    }

    [Theory]
    [InlineData(CatalogContentType.Root)]
    [InlineData(CatalogContentType.Catalog)]
    public void GetContentKey_returns_empty_when_unsupported_content_type(CatalogContentType contentType)
    {
        var contentLink = CreateCommerceContentLink();
        A.CallTo(() => _fakeReferenceConverter.GetContentType(contentLink)).Returns(contentType);

        var result = _contentKeyProvider.GetContentKey(contentLink);

        Assert.Equal(ContentKeyResult.Empty, result);
    }

    [Fact]
    public void GetContentKey_returns_empty_when_no_code()
    {
        var contentLink = CreateCommerceContentLink();
        A.CallTo(() => _fakeReferenceConverter.GetCode(contentLink)).Returns(string.Empty);

        var result = _contentKeyProvider.GetContentKey(contentLink);

        Assert.Equal(ContentKeyResult.Empty, result);
    }

    [Fact]
    public void GetContentKey_returns_key_with_code_and_provider_name()
    {
        var contentLink = CreateCommerceContentLink();
        var code = Guid.NewGuid().ToString();
        A.CallTo(() => _fakeReferenceConverter.GetCode(contentLink)).Returns(code);

        var result = _contentKeyProvider.GetContentKey(contentLink);

        Assert.Contains(code, result.Key);
        Assert.Contains(contentLink.ProviderName, result.Key);
    }

    private void InitializeFakes()
    {
        var contentType = ValidCatalogContentTypes[new Random(DateTime.Now.Millisecond).Next(0, 1)];
        A.CallTo(() => _fakeReferenceConverter.GetContentType(A<ContentReference>._)).Returns(contentType);
    }

    private static ContentReference CreateCommerceContentLink()
    {
        var random = new Random(DateTime.Now.Millisecond);
        return new ContentReference(random.Next(), random.Next(), "CatalogContent");
    }
}
