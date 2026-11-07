using System;
using System.Linq;
using System.Text.RegularExpressions;
using FakeItEasy;
using Geta.NotFoundHandler.Core.Providers.RegexRedirects;
using Geta.NotFoundHandler.Data;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Geta.NotFoundHandler.Tests.Providers;

public class MemoryCacheRegexRedirectRepositoryTests
{
    [Fact]
    public void GetAll_builds_each_redirect_once_however_often_it_is_read()
    {
        var builds = 0;
        var loader = A.Fake<IRegexRedirectLoader>();
        // Lazy, like the sql repository, where building one means compiling its regex.
        A.CallTo(() => loader.GetAll()).Returns(Enumerable.Range(0, 3).Select(_ =>
        {
            builds++;
            return new RegexRedirect(Guid.NewGuid(), new Regex("a"), "/b", 1, 0, DateTime.UtcNow, DateTime.UtcNow);
        }));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new MemoryCacheRegexRedirectRepository(
            A.Fake<IRepository<RegexRedirect>>(),
            loader,
            A.Fake<IRegexRedirectOrderUpdater>(),
            cache);

        var first = sut.GetAll().ToList();
        var second = sut.GetAll().ToList();

        Assert.Equal(3, builds);
        Assert.Same(first[0], second[0]);
    }
}
