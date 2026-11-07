// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FakeItEasy;
using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Providers.RegexRedirects;
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
    /// https://github.com/Geta/geta-notfoundhandler/issues/208
    /// </summary>
    public class RedirectLoopTests
    {
        private const int MaxHops = 5;

        private readonly NotFoundHandlerOptions _options = new() { MaxRedirectHops = MaxHops };
        private readonly IRegexRedirectLoader _regexRedirects = A.Fake<IRegexRedirectLoader>();
        private readonly CustomRedirectCollection _redirects;
        private readonly RequestHandler _sut;

        public RedirectLoopTests()
        {
            A.CallTo(() => _regexRedirects.GetAll()).Returns(new List<RegexRedirect>());
            var regexHandler = new RegexRedirectNotFoundHandler(_regexRedirects,
                                                                NullLogger<RegexRedirectNotFoundHandler>.Instance,
                                                                Options.Create(_options));
            _redirects = new CustomRedirectCollection(new INotFoundHandler[] { regexHandler });
            var redirectHandler = new CustomRedirectHandler();
            redirectHandler.Set(_redirects);

            _sut = new RequestHandler(redirectHandler,
                                      A.Fake<IRequestLogger>(),
                                      Options.Create(_options),
                                      NullLogger<RequestHandler>.Instance,
                                      new RedirectLoopGuard(Options.Create(_options)));
        }

        [Fact]
        public void Rules_pointing_into_each_other_stop_at_the_limit()
        {
            _redirects.Add(new CustomRedirect("/a", "/b/c"));
            _redirects.Add(new CustomRedirect("/b", "/a/d"));

            var followed = Follow("https://example.com/a");

            Assert.Equal(MaxHops, followed.Count);
            Assert.Equal(new[] { "/b/c", "/a/d/c", "/b/c/d/c" }, followed.Take(3));
        }

        [Fact]
        public void Rules_sending_a_url_back_and_forth_stop_at_the_limit()
        {
            _redirects.Add(new CustomRedirect("/a", "/b"));
            _redirects.Add(new CustomRedirect("/b", "/a"));

            Assert.Equal(MaxHops, Follow("https://example.com/a").Count);
        }

        [Fact]
        public void A_regex_rule_matching_its_own_result_stops_at_the_limit()
        {
            GivenRegexRule("^/(.*)$", "/en/$1");

            var followed = Follow("https://example.com/foo");

            Assert.Equal(MaxHops, followed.Count);
            Assert.Equal("/en/foo", followed[0]);
        }

        [Fact]
        public void A_regex_rule_matching_its_own_result_is_fine_when_the_new_url_exists()
        {
            GivenRegexRule("^/(.*)$", "/en/$1");

            for (var i = 0; i < MaxHops * 3; i++)
            {
                Assert.True(_sut.HandleRequest(null, new Uri("https://example.com/foo"), out var redirect));
                Assert.Equal("/en/foo", redirect.NewUrl);
            }
        }

        [Fact]
        public void Many_requests_for_a_chain_without_a_loop_are_all_redirected()
        {
            _redirects.Add(new CustomRedirect("/a", "/b"));
            _redirects.Add(new CustomRedirect("/b", "/c"));

            for (var i = 0; i < MaxHops * 3; i++)
            {
                Assert.Equal(new[] { "/b", "/c" }, Follow("https://example.com/a"));
            }
        }

        /// <summary>
        /// A page moved to /b and back leaves both rules behind, and /a exists again. Only /b is ever not found.
        /// </summary>
        [Fact]
        public void Rules_pointing_at_each_other_are_fine_when_one_url_exists()
        {
            _redirects.Add(new CustomRedirect("/a", "/b"));
            _redirects.Add(new CustomRedirect("/b", "/a"));

            for (var i = 0; i < MaxHops * 3; i++)
            {
                Assert.True(_sut.HandleRequest(null, new Uri("https://example.com/b"), out var redirect));
                Assert.Equal("/a", redirect.NewUrl);
            }
        }

        [Fact]
        public void A_chain_as_long_as_the_limit_is_followed_to_the_end()
        {
            for (var i = 0; i < MaxHops; i++)
            {
                _redirects.Add(new CustomRedirect($"/p{i}", $"/p{i + 1}"));
            }

            var followed = Follow("https://example.com/p0");

            Assert.Equal($"/p{MaxHops}", followed.Last());
        }

        [Fact]
        public void A_corrected_rule_works_right_after_being_stopped()
        {
            _redirects.Add(new CustomRedirect("/a", "/b"));
            _redirects.Add(new CustomRedirect("/b", "/a"));
            Follow("https://example.com/a");

            _redirects.Add(new CustomRedirect("/b", "/c"));

            Assert.Equal(new[] { "/b", "/c" }, Follow("https://example.com/a"));
        }

        [Fact]
        public void Going_from_http_to_https_on_the_same_url_does_not_add_up_over_requests()
        {
            var guard = new RedirectLoopGuard(Options.Create(new NotFoundHandlerOptions { MaxRedirectHops = 1 }));

            for (var i = 0; i < 5; i++)
            {
                Assert.True(guard.TryRedirect(new Uri("http://example.com/a"), "https://example.com/a"));
            }
        }

        [Fact]
        public void A_redirect_into_a_loop_from_outside_does_not_lower_its_count()
        {
            var guard = new RedirectLoopGuard(Options.Create(new NotFoundHandlerOptions { MaxRedirectHops = 2 }));

            Assert.True(guard.TryRedirect(new Uri("https://example.com/a"), "/b"));
            Assert.True(guard.TryRedirect(new Uri("https://example.com/b"), "/a"));
            Assert.True(guard.TryRedirect(new Uri("https://example.com/old"), "/a"));

            Assert.False(guard.TryRedirect(new Uri("https://example.com/a"), "/b"));
        }

        [Fact]
        public void The_limit_can_be_turned_off()
        {
            _options.MaxRedirectHops = 0;
            var guard = new RedirectLoopGuard(Options.Create(_options));
            var url = new Uri("https://example.com/a");

            for (var i = 0; i < 100; i++)
            {
                Assert.True(guard.TryRedirect(url, "/a"));
            }
        }

        [Theory]
        [InlineData("https://example.com/b", "/a/", "https://example.com/a/")]
        [InlineData("https://example.com/b", "https://example.com/a", "http://example.com/a")]
        [InlineData("https://example.com/b", "//example.com/a", "https://example.com/a")]
        [InlineData("https://example.com/b", "/A", "https://example.com/A")]
        [InlineData("https://example.com/b", "/a b", "https://example.com/a%20b")]
        [InlineData("https://example.com/b", "/æ?x=1", "https://example.com/%C3%A6?x=1")]
        [InlineData("https://example.com/b", "/a#top", "https://example.com/a")]
        [InlineData("https://example.com/b", "/a[b]", "https://example.com/a%5Bb%5D")]
        [InlineData("https://example.com/b", "/a|b", "https://example.com/a%7Cb")]
        [InlineData("https://example.com/b", "/a?q=it's", "https://example.com/a?q=it%27s")]
        [InlineData("https://example.com/b", "https://bücher.example.com/a", "https://xn--bcher-kva.example.com/a")]
        public void A_hop_is_counted_however_the_new_url_is_written(string url, string newUrl, string nextRequest)
        {
            var options = Options.Create(new NotFoundHandlerOptions { MaxRedirectHops = 1 });
            var guard = new RedirectLoopGuard(options);

            Assert.True(guard.TryRedirect(new Uri(url), newUrl));

            Assert.False(guard.TryRedirect(new Uri(nextRequest), "/next"));
        }

        /// <summary>
        /// None of these is a loop, so separate requests for them mustn't add up.
        /// </summary>
        [Theory]
        [InlineData("https://example.com/Foo", "/foo")]
        [InlineData("http://example.com/a", "https://example.com/a")]
        [InlineData("https://example.com/a%2Fb", "/a/b")]
        public void A_url_redirecting_to_another_spelling_of_itself_is_not_a_loop(string url, string newUrl)
        {
            var guard = new RedirectLoopGuard(Options.Create(_options));

            for (var i = 0; i < MaxHops * 3; i++)
            {
                Assert.True(guard.TryRedirect(new Uri(url), newUrl));
            }
        }

        [Fact]
        public void A_url_redirecting_to_itself_with_a_fragment_is_a_loop()
        {
            var guard = new RedirectLoopGuard(Options.Create(_options));
            var url = new Uri("https://example.com/a");

            for (var i = 0; i < MaxHops; i++)
            {
                Assert.True(guard.TryRedirect(url, "/a#top"));
            }

            Assert.False(guard.TryRedirect(url, "/a#top"));
        }

        [Fact]
        public void A_hop_to_another_url_or_host_is_not_counted_on_this_url()
        {
            var options = Options.Create(new NotFoundHandlerOptions { MaxRedirectHops = 1 });
            var guard = new RedirectLoopGuard(options);

            Assert.True(guard.TryRedirect(new Uri("https://example.com/b"), "https://other.example.com/a"));

            Assert.True(guard.TryRedirect(new Uri("https://example.com/a"), "/next"));
        }

        [Theory]
        [InlineData("http://")]
        [InlineData("mailto:someone@example.com")]
        [InlineData("javascript:void(0)")]
        public void A_new_url_that_is_not_a_web_url_is_still_redirected(string newUrl)
        {
            var guard = new RedirectLoopGuard(Options.Create(_options));

            Assert.True(guard.TryRedirect(new Uri("https://example.com/a"), newUrl));
        }

        private void GivenRegexRule(string oldUrlRegex, string newUrlFormat)
        {
            var rule = new RegexRedirect(Guid.NewGuid(), new Regex(oldUrlRegex), newUrlFormat, 1, 0, DateTime.UtcNow, DateTime.UtcNow);
            A.CallTo(() => _regexRedirects.GetAll()).Returns(new List<RegexRedirect> { rule });
        }

        /// <summary>
        /// Requests urls the way a browser would when none of them exist, until it isn't redirected.
        /// </summary>
        private List<string> Follow(string url)
        {
            var followed = new List<string>();
            var current = new Uri(url);

            while (followed.Count < MaxHops * 5 && _sut.HandleRequest(null, current, out var redirect))
            {
                followed.Add(redirect.NewUrl);
                current = new Uri(current, redirect.NewUrl);
            }

            return followed;
        }
    }
}
