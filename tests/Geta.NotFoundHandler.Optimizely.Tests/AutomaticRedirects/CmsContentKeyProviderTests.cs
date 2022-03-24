using EPiServer.Core;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests.AutomaticRedirects;

public class CmsContentKeyProviderTests
{
    private readonly CmsContentKeyProvider _cmsContentKeyProvider = new();

    [Fact]
    public void GetContentKey_returns_empty_when_link_is_empty()
    {
        var result = _cmsContentKeyProvider.GetContentKey(ContentReference.EmptyReference);

        Assert.Equal(ContentKeyResult.Empty, result);
    }

    [Fact]
    public void GetContentKey_returns_empty_when_not_cms_provider()
    {
        var nonCmsLink = new ContentReference(123, 321, "NonCmsProvider");

        var result = _cmsContentKeyProvider.GetContentKey(nonCmsLink);

        Assert.Equal(ContentKeyResult.Empty, result);
    }

    [Fact]
    public void GetContentKey_returns_non_empty_result_when_cms_provider()
    {
        var cmsLink = new ContentReference(122);

        var result = _cmsContentKeyProvider.GetContentKey(cmsLink);

        Assert.NotEqual(ContentKeyResult.Empty, result);
    }

    [Fact]
    public void GetContentKey_returns_key_with_link_id()
    {
        var linkId = 1233;
        var link = new ContentReference(linkId);

        var result = _cmsContentKeyProvider.GetContentKey(link);

        Assert.Equal(result.Key, linkId.ToString());
    }

    [Fact]
    public void GetContentKey_returns_key_without_version_id()
    {
        var versionId = 431;
        var link = new ContentReference(123, versionId);

        var result = _cmsContentKeyProvider.GetContentKey(link);

        Assert.DoesNotContain(versionId.ToString(), result.Key);
    }
}
