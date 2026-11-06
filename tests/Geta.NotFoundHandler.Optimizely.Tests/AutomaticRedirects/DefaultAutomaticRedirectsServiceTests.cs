using System.Collections.Generic;
using FakeItEasy;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Geta.NotFoundHandler.Optimizely.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests.AutomaticRedirects;

public class DefaultAutomaticRedirectsServiceTests
{
    private readonly IRedirectsService _redirectsService = A.Fake<IRedirectsService>();
    private readonly DefaultAutomaticRedirectsService _sut;

    public DefaultAutomaticRedirectsServiceTests()
    {
        var redirectBuilder = new RedirectBuilder(
            new OptionsWrapper<OptimizelyNotFoundHandlerOptions>(new OptimizelyNotFoundHandlerOptions()));
        _sut = new DefaultAutomaticRedirectsService(_redirectsService, redirectBuilder);
    }

    /// <summary>
    /// Both writes raise RedirectsUpdated even when empty, which reloads the redirects on every instance.
    /// </summary>
    [Fact]
    public void CreateRedirects_writes_nothing_when_content_has_no_history()
    {
        _sut.CreateRedirects(new List<ContentUrlHistory>());

        A.CallTo(() => _redirectsService.AddOrUpdate(A<IEnumerable<CustomRedirect>>._)).MustNotHaveHappened();
        A.CallTo(() => _redirectsService.DeleteByOldUrl(A<IEnumerable<string>>._)).MustNotHaveHappened();
    }
}
