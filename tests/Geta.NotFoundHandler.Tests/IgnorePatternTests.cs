// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using FakeItEasy;
using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Core.Suggestions;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Geta.NotFoundHandler.Tests
{
    /// <summary>
    /// https://github.com/Geta/geta-notfoundhandler/issues/197
    /// </summary>
    public class IgnorePatternTests
    {
        private readonly NotFoundHandlerOptions _options = new();
        private readonly INotFoundHandler _provider = A.Fake<INotFoundHandler>();
        private readonly IRequestLogger _requestLogger = A.Fake<IRequestLogger>();
        private readonly CustomRedirectCollection _redirects;
        private readonly RequestHandler _sut;

        public IgnorePatternTests()
        {
            A.CallTo(() => _provider.RewriteUrl(A<string>._)).Returns(RewriteResult.Empty);
            A.CallTo(() => _requestLogger.ShouldLogRequest(A<string>._)).Returns(true);

            _redirects = new CustomRedirectCollection(new[] { _provider });
            var redirectHandler = new CustomRedirectHandler();
            redirectHandler.Set(_redirects);

            _sut = new RequestHandler(redirectHandler,
                                      _requestLogger,
                                      Options.Create(_options),
                                      NullLogger<RequestHandler>.Instance,
                                      new RedirectLoopGuard(Options.Create(_options)));
        }

        [Theory]
        [InlineData("/api/*", "/api/")]
        [InlineData("/api/*", "/api/users")]
        [InlineData("/api/*", "/api/users/1")]
        [InlineData("/api/*", "/api/users?id=1")]
        [InlineData("/api/*", "/API/Users")]
        [InlineData("/API/*", "/api/users")]
        [InlineData("/a b/*", "/a%20b/c")]
        [InlineData("*.php", "/wp/login.php")]
        [InlineData("/*/admin", "/en/admin")]
        [InlineData("/*/admin", "/en/no/admin")]
        [InlineData("/search?q=*", "/search?q=a")]
        [InlineData("/?*", "/?a=1")]
        [InlineData("/*?*", "/a?b=1")]
        [InlineData("*?x=1", "/a?x=1")]
        [InlineData(@"/a\b/*", "/a%5Cb/c")]
        [InlineData("https://example.com/api/*", "/api/users")]
        [InlineData("HTTPS://EXAMPLE.COM/api/*", "/api/users")]
        public void A_url_matching_a_pattern_is_not_logged(string pattern, string url)
        {
            _redirects.Add(new CustomRedirect(pattern, RedirectState.Ignored));

            Request(url);

            AssertNotLogged();
        }

        [Theory]
        [InlineData("/api/*", "/api")]
        [InlineData("/api/*", "/apis/users")]
        [InlineData("/api/*", "/en/api/users")]
        [InlineData("*.php", "/wp/login.phpx")]
        [InlineData("/*/admin", "/admin")]
        [InlineData("https://example.com/api/*", "/other/users")]
        [InlineData("https://other.com/api/*", "/api/users")]
        [InlineData("*.com*", "/a")]
        [InlineData("*.*", "/a")]
        [InlineData("*example*", "/a")]
        [InlineData("*s*", "/a")]
        [InlineData("/a?b=*", "/axb=1")]
        [InlineData("/?*", "/a")]
        [InlineData(@"/a\*", "/a")]
        [InlineData(@"/a\b/*", "/ab/c")]
        [InlineData("/a%2A", "/ab")]
        public void A_url_not_matching_a_pattern_is_logged(string pattern, string url)
        {
            _redirects.Add(new CustomRedirect(pattern, RedirectState.Ignored));

            Request(url);

            AssertLogged();
        }

        [Theory]
        [InlineData("*")]
        [InlineData("/*")]
        [InlineData("/**")]
        [InlineData("/*/*")]
        [InlineData("http*")]
        [InlineData("https*")]
        [InlineData("https://*")]
        [InlineData("https://*/*")]
        [InlineData("https://example.com*")]
        [InlineData("https://example.com/*")]
        [InlineData("HTTPS://EXAMPLE.COM/*")]
        [InlineData("http*://example.com/*")]
        public void Something_matching_everything_is_not_a_pattern(string url)
        {
            _redirects.Add(new CustomRedirect(url, RedirectState.Ignored));

            Request("/a/b");

            AssertLogged();
        }

        [Fact]
        public void An_ignored_url_without_a_wildcard_still_only_stops_itself_being_logged()
        {
            _redirects.Add(new CustomRedirect("/api", RedirectState.Ignored));

            Request("/api");
            AssertNotLogged();

            Request("/api/users");
            AssertLogged();
        }

        [Fact]
        public void A_saved_redirect_with_a_wildcard_is_not_a_pattern()
        {
            _redirects.Add(new CustomRedirect("/api/*", "/new"));

            Request("/api/users");

            AssertLogged();
        }

        [Fact]
        public void A_redirect_for_a_url_matching_a_pattern_still_applies()
        {
            _redirects.Add(new CustomRedirect("/api/*", RedirectState.Ignored));
            _redirects.Add(new CustomRedirect("/api/old", "/api/new"));

            Assert.Equal("/api/new", Request("/api/old"));
            Assert.Equal("/api/new/child", Request("/api/old/child"));
        }

        [Fact]
        public void A_shorter_redirect_still_applies_to_a_url_matching_a_pattern()
        {
            _redirects.Add(new CustomRedirect("/a/b/*", RedirectState.Ignored));
            _redirects.Add(new CustomRedirect("/a", "/new"));

            Assert.Equal("/new/b/c", Request("/a/b/c"));
        }

        [Fact]
        public void A_regex_redirect_still_applies_to_a_url_matching_a_pattern()
        {
            _redirects.Add(new CustomRedirect("/api/*", RedirectState.Ignored));
            A.CallTo(() => _provider.RewriteUrl(A<string>._)).Returns(new RewriteResult("/new", RedirectType.Temporary));

            Assert.Equal("/new", Request("/api/users"));
        }

        [Fact]
        public void A_pattern_replacing_another_row_is_picked_up()
        {
            _redirects.Add(new CustomRedirect("/a/*", "/new"));
            Request("/a/b");
            AssertLogged();

            _redirects.Add(new CustomRedirect("/a/*", RedirectState.Ignored));
            Request("/a/c");

            AssertNotLogged();
        }

        [Fact]
        public void Ignoring_a_pattern_deletes_the_suggestions_matching_it()
        {
            var repository = A.Fake<ISuggestionRepository>();
            var sut = CreateSuggestionService(repository);

            sut.IgnoreSuggestion("/api/*");

            A.CallTo(() => repository.DeleteMatching("/api/*")).MustHaveHappenedOnceExactly();
        }

        [Theory]
        [InlineData("/old")]
        [InlineData("/*")]
        [InlineData("https://example.com/*")]
        public void Ignoring_a_url_only_deletes_the_suggestions_for_that_url(string url)
        {
            var repository = A.Fake<ISuggestionRepository>();
            var sut = CreateSuggestionService(repository);

            sut.IgnoreSuggestion(url);

            A.CallTo(() => repository.DeleteForRequest(url)).MustHaveHappenedOnceExactly();
            A.CallTo(() => repository.DeleteMatching(A<string>._)).MustNotHaveHappened();
        }

        private DefaultSuggestionService CreateSuggestionService(ISuggestionRepository repository)
        {
            return new DefaultSuggestionService(A.Fake<ISuggestionLoader>(),
                                                A.Fake<IRedirectsService>(),
                                                repository,
                                                Options.Create(_options));
        }

        private string Request(string url)
        {
            Fake.ClearRecordedCalls(_requestLogger);

            return _sut.HandleRequest(null, new Uri(new Uri("https://example.com"), url), out var redirect)
                ? redirect.NewUrl
                : null;
        }

        private void AssertLogged()
        {
            A.CallTo(() => _requestLogger.LogRequest(A<string>._, A<string>._)).MustHaveHappenedOnceExactly();
        }

        private void AssertNotLogged()
        {
            A.CallTo(() => _requestLogger.LogRequest(A<string>._, A<string>._)).MustNotHaveHappened();
        }
    }
}
