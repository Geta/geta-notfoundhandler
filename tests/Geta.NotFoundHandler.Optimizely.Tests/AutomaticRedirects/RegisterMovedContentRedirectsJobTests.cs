using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Geta.NotFoundHandler.Optimizely.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests.AutomaticRedirects;

public class RegisterMovedContentRedirectsJobTests
{
    private readonly IAutomaticRedirectsService _redirectsService = A.Fake<IAutomaticRedirectsService>();
    private readonly IContentUrlHistoryLoader _loader = A.Fake<IContentUrlHistoryLoader>();

    [Fact]
    public void Execute_processes_every_page()
    {
        A.CallTo(() => _loader.GetAllMoved(0, 2)).Returns(Moved("a", "b"));
        A.CallTo(() => _loader.GetAllMoved(2, 2)).Returns(Moved("c"));
        var job = CreateJob(batchSize: 2);

        job.Execute();

        A.CallTo(() => _redirectsService.CreateRedirects(A<IReadOnlyCollection<ContentUrlHistory>>._))
            .MustHaveHappened(3, Times.Exactly);
        A.CallTo(() => _loader.GetAllMoved(0, 2)).MustHaveHappened();
        A.CallTo(() => _loader.GetAllMoved(2, 2)).MustHaveHappened();
    }

    [Fact]
    public void Execute_does_not_request_next_page_after_a_partial_page()
    {
        A.CallTo(() => _loader.GetAllMoved(0, 5)).Returns(Moved("a", "b"));
        var job = CreateJob(batchSize: 5);

        job.Execute();

        A.CallTo(() => _loader.GetAllMoved(5, 5)).MustNotHaveHappened();
    }

    [Fact]
    public void Execute_stops_after_empty_page_when_total_is_exact_multiple_of_batch_size()
    {
        A.CallTo(() => _loader.GetAllMoved(0, 2)).Returns(Moved("a", "b"));
        A.CallTo(() => _loader.GetAllMoved(2, 2)).Returns(Moved()); // full last page forces one extra, empty fetch
        var job = CreateJob(batchSize: 2);

        job.Execute();

        A.CallTo(() => _loader.GetAllMoved(2, 2)).MustHaveHappened();
        A.CallTo(() => _loader.GetAllMoved(4, 2)).MustNotHaveHappened();
        A.CallTo(() => _redirectsService.CreateRedirects(A<IReadOnlyCollection<ContentUrlHistory>>._))
            .MustHaveHappened(2, Times.Exactly);
    }

    [Fact]
    public void Execute_clamps_non_positive_batch_size_to_one()
    {
        var job = CreateJob(batchSize: 0);

        job.Execute();

        // A configured 0/negative would page with "FETCH NEXT 0 ROWS ONLY" (invalid SQL); the page size must clamp to >= 1.
        A.CallTo(() => _loader.GetAllMoved(0, 0)).MustNotHaveHappened();
        A.CallTo(() => _loader.GetAllMoved(0, 1)).MustHaveHappened();
    }

    [Fact]
    public void Execute_continues_when_a_single_item_fails()
    {
        A.CallTo(() => _loader.GetAllMoved(0, 10)).Returns(Moved("good", "bad", "good"));
        A.CallTo(() => _redirectsService.CreateRedirects(A<IReadOnlyCollection<ContentUrlHistory>>.That.Matches(
                x => x.Count == 1 && x.First().ContentKey == "bad")))
            .Throws(new System.InvalidOperationException("boom"));
        var job = CreateJob(batchSize: 10);

        var result = job.Execute();

        // All three are attempted even though the middle one throws.
        A.CallTo(() => _redirectsService.CreateRedirects(A<IReadOnlyCollection<ContentUrlHistory>>._))
            .MustHaveHappened(3, Times.Exactly);
        Assert.Contains("failed", result);
    }

    private RegisterMovedContentRedirectsJob CreateJob(int batchSize)
    {
        var options = Options.Create(new OptimizelyNotFoundHandlerOptions { MovedContentBatchSize = batchSize });
        return new RegisterMovedContentRedirectsJob(_redirectsService, _loader, options);
    }

    private static IEnumerable<(string contentKey, IReadOnlyCollection<ContentUrlHistory> histories)> Moved(params string[] keys)
    {
        foreach (var key in keys)
        {
            yield return (key, new List<ContentUrlHistory> { new() { ContentKey = key } });
        }
    }
}
