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

    [Fact]
    public void UpdateOrder_makes_the_next_read_load_the_new_order()
    {
        var loader = A.Fake<IRegexRedirectLoader>();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new MemoryCacheRegexRedirectRepository(
            A.Fake<IRepository<RegexRedirect>>(),
            loader,
            A.Fake<IRegexRedirectOrderUpdater>(),
            cache);

        sut.GetAll();
        sut.UpdateOrder();
        sut.GetAll();

        A.CallTo(() => loader.GetAll()).MustHaveHappenedTwiceExactly();
    }
}
