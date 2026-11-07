using System;
using System.Linq;
using EPiServer.Core;
using FakeItEasy;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests.AutomaticRedirects;

public class IndexContentUrlsJobTests
{
    private readonly ContentUrlIndexer _indexer = A.Fake<ContentUrlIndexer>();
    private readonly ContentLinkLoader _linkLoader = A.Fake<ContentLinkLoader>();
    private readonly ILogger<IndexContentUrlsJob> _logger = A.Fake<ILogger<IndexContentUrlsJob>>();

    /// <summary>
    /// https://github.com/Geta/geta-notfoundhandler/issues/180
    /// </summary>
    [Fact]
    public void Execute_returns_a_summary_and_logs_the_exceptions()
    {
        var links = new[] { new ContentReference(1), new ContentReference(2), new ContentReference(3) };
        A.CallTo(() => _linkLoader.GetAllLinks()).Returns(links);
        A.CallTo(() => _indexer.IndexContentUrls(links[1])).Throws(new InvalidOperationException("the details of the failure"));
        var job = new IndexContentUrlsJob(_indexer, _linkLoader, _logger);

        var result = job.Execute();

        Assert.Contains("Processed 3", result);
        Assert.Contains("successful 2", result);
        Assert.Contains("failed: 1", result);
        Assert.DoesNotContain("the details of the failure", result);
        Assert.Equal(1, Fake.GetCalls(_logger).Count(call => call.Method.Name == nameof(ILogger.Log) && call.GetArgument<LogLevel>(0) == LogLevel.Error));
    }

    [Fact]
    public void Execute_keeps_the_result_short_however_many_fail()
    {
        var links = Enumerable.Range(1, 1000).Select(id => new ContentReference(id)).ToList();
        A.CallTo(() => _linkLoader.GetAllLinks()).Returns(links);
        A.CallTo(() => _indexer.IndexContentUrls(A<ContentReference>._)).Throws(new InvalidOperationException("failure"));
        var job = new IndexContentUrlsJob(_indexer, _linkLoader, _logger);

        var result = job.Execute();

        Assert.True(result.Length < 500, $"The result was {result.Length} characters.");
        Assert.Contains("failed: 1000", result);
    }
}
