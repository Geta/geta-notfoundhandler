using System;
using System.Linq;
using EPiServer.Core;
using FakeItEasy;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests.AutomaticRedirects;

public class ContentUrlIndexerTests
{
    private readonly Random _random = new Random(DateTime.Now.Millisecond);
    private readonly ContentUrlIndexer _indexer;
    private readonly ContentKeyGenerator _fakeContentKeyGenerator;
    private readonly ContentUrlLoader _fakeContentUrlLoader;
    private readonly IRepository<ContentUrlHistory> _fakeContentUrlHistoryRepository;
    private readonly IContentUrlHistoryLoader _fakeContentUrlHistoryLoader;

    public ContentUrlIndexerTests()
    {
        _fakeContentKeyGenerator = A.Fake<ContentKeyGenerator>();
        _fakeContentUrlLoader = A.Fake<ContentUrlLoader>();
        _fakeContentUrlHistoryRepository = A.Fake<IRepository<ContentUrlHistory>>();
        _fakeContentUrlHistoryLoader = A.Fake<IContentUrlHistoryLoader>();

        _indexer = new ContentUrlIndexer(_fakeContentKeyGenerator,
                                         _fakeContentUrlLoader,
                                         _fakeContentUrlHistoryRepository,
                                         _fakeContentUrlHistoryLoader);

        InitFakes();
    }

    [Fact]
    public void IndexContentUrl_does_not_save_history_when_no_key()
    {
        var contentLink = CreateContentLink();
        A.CallTo(() => _fakeContentKeyGenerator.GetContentKey(contentLink)).Returns(ContentKeyResult.Empty);

        _indexer.IndexContentUrl(contentLink);

        AssertNotSaved();
    }

    [Fact]
    public void IndexContentUrl_does_not_save_history_when_already_registered()
    {
        var contentLink = CreateContentLink();
        A.CallTo(() => _fakeContentUrlHistoryLoader.IsRegistered(A<ContentUrlHistory>._)).Returns(true);

        _indexer.IndexContentUrl(contentLink);

        AssertNotSaved();
    }

    [Fact]
    public void IndexContentUrl_saves_history_with_key_and_urls()
    {
        var contentLink = CreateContentLink();
        var key = Guid.NewGuid().ToString();
        var urls = A.CollectionOfDummy<TypedUrl>(_random.Next(2, 10));
        A.CallTo(() => _fakeContentKeyGenerator.GetContentKey(contentLink)).Returns(new ContentKeyResult(key));
        A.CallTo(() => _fakeContentUrlLoader.GetUrls(contentLink)).Returns(urls);

        _indexer.IndexContentUrl(contentLink);

        var expected = new ContentUrlHistory { ContentKey = key, Urls = urls };
        AssertSaved(expected);
    }

    private void InitFakes()
    {
        A.CallTo(() => _fakeContentKeyGenerator.GetContentKey(A<ContentReference>._))
            .Returns(new ContentKeyResult(Guid.NewGuid().ToString()));
        A.CallTo(() => _fakeContentUrlHistoryLoader.IsRegistered(A<ContentUrlHistory>._)).Returns(false);
    }

    private ContentReference CreateContentLink()
    {
        return new ContentReference(_random.Next());
    }

    private void AssertNotSaved()
    {
        A.CallTo(() => _fakeContentUrlHistoryRepository.Save(A<ContentUrlHistory>._)).MustNotHaveHappened();
    }

    private void AssertSaved(ContentUrlHistory expected)
    {
        A.CallTo(() => _fakeContentUrlHistoryRepository.Save(
                     A<ContentUrlHistory>.That.Matches(x => x.ContentKey == expected.ContentKey
                                                            && x.Urls.SequenceEqual(expected.Urls))))
            .MustNotHaveHappened();
    }
}
