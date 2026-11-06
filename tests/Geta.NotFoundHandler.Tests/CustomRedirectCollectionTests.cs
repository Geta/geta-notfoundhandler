using System;
using System.Collections.Generic;
using System.Web;
using FakeItEasy;
using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Tests.Base;
using Xunit;

namespace Geta.NotFoundHandler.Tests
{
    public class CustomRedirectCollectionTests
    {
        private static readonly Uri DefaultNewUri = new Uri("http://example.com/new");
        private static readonly Uri DefaultOldUri = new Uri("http://example.com/old");
        private static readonly string RelativeUrlWithParams = "/old?param1=value1&param2=value2";
        private static readonly string RelativeUrlWithSlashAndParams = "/old/?param1=value1&param2=value2";

        private readonly CustomRedirectCollection _sut;
        private readonly List<INotFoundHandler> _providers;

        public CustomRedirectCollectionTests()
        {
            _providers = new List<INotFoundHandler>();
            _sut = new CustomRedirectCollection(_providers);
        }

        public static IEnumerable<object[]> OldUrls => new[]
        {
            new object[] { DefaultOldUri.AbsoluteUri, DefaultOldUri.AbsoluteUri },
            new object[] { DefaultOldUri.PathAndQuery, DefaultOldUri.PathAndQuery },
            new object[] { HttpUtility.HtmlEncode(RelativeUrlWithParams), RelativeUrlWithParams },
            new object[] { HttpUtility.HtmlEncode(RelativeUrlWithSlashAndParams), RelativeUrlWithSlashAndParams }
        };

        [Theory]
        [MemberData(nameof(OldUrls))]
        public void Find_finds_redirect(string storedUrl, string notFoundUrl)
        {
            var redirect = new CustomRedirect(storedUrl, DefaultNewUri.ToString());
            _sut.Add(redirect);

            var actual = _sut.Find(notFoundUrl.ToUri());

            Assert.Equal(redirect.NewUrl, actual.NewUrl);
        }

        [Fact]
        public void Find_handles_path_and_query_parameters_when_matching_sub_segment()
        {
            var collection = new CustomRedirectCollection
            {
                new CustomRedirect("/oldsegment1/", "/newsegment1/?test=r"),
            };

            var urlToFind = "/oldsegment1/oldsegment2/?test=q";
            var expected = "/newsegment1/oldsegment2/?test=q&test=r";

            var actual = collection.Find(urlToFind.ToUri());

            Assert.Equal(expected, actual.NewUrl);
        }

        [Fact]
        public void Find_finds_redirect_when_not_found_url_starts_with_stored_url_and_WildCardSkipAppend_enabled()
        {
            var storedUrl = "/old";
            var redirect = new CustomRedirect(storedUrl, DefaultNewUri.PathAndQuery)
            {
                WildCardSkipAppend = true
            };
            _sut.Add(redirect);

            var actual = _sut.Find($"{storedUrl}/page".ToUri());

            Assert.Equal(redirect.NewUrl, actual.NewUrl);
        }

        [Fact]
        public void Find_finds_most_specific_redirect_when_not_found_url_starts_with_stored_url()
        {
            var collection = new CustomRedirectCollection
            {
                new CustomRedirect("/oldsegment1/", "/newsegment1/"),
                new CustomRedirect("/oldsegment1/oldsegment2/", "/newsegment1/newsegment2/"),
            };

            var urlToFind = "/oldsegment1/oldsegment2/?test=q";
            var expected = "/newsegment1/newsegment2/?test=q";

            var actual = collection.Find(urlToFind.ToUri());

            Assert.Equal(expected, actual.NewUrl);
        }

        [Fact]
        public void Find_finds_redirect_and_appends_relative_path_when_not_found_url_starts_with_stored_url()
        {
            var storedUrl = "/old";
            var redirect = new CustomRedirect(storedUrl, DefaultNewUri.PathAndQuery);
            var newRedirectUrlWithPath = $"{redirect.NewUrl}/page";
            _sut.Add(redirect);

            var actual = _sut.Find($"{storedUrl}/page".ToUri());

            Assert.Equal(newRedirectUrlWithPath, actual.NewUrl);
        }

        [Fact]
        public void Find_doesnt_find_redirect_when_not_found_url_starts_with_stored_url_and_redirect_is_ignored()
        {
            var storedUrl = "/old";
            var redirect = new CustomRedirect(storedUrl, DefaultNewUri.PathAndQuery)
            {
                State = (int)RedirectState.Ignored
            };
            _sut.Add(redirect);

            var actual = _sut.Find($"{storedUrl}/page".ToUri());

            Assert.Null(actual);
        }

        [Fact]
        public void Find_finds_redirect_in_provider()
        {
            var oldUrl = "http://example.com/old";
            var newUrl = "http://example.com/new";
            WithProvider(oldUrl, newUrl);

            var actual = _sut.Find(oldUrl.ToUri());

            Assert.Equal(newUrl, actual.NewUrl);
        }

        [Fact]
        public void Find_does_not_find_redirect_in_provider_when_not_found_in_provider()
        {
            var oldUrl = "http://example.com/old";
            WithProvider(oldUrl, null);

            var actual = _sut.Find(oldUrl.ToUri());

            Assert.Null(actual);
        }

        [Fact]
        public void Add_updates_already_added_redirect()
        {
            var redirect = new CustomRedirect(DefaultOldUri.PathAndQuery, DefaultNewUri.ToString());
            _sut.Add(redirect);
            var updatedUrl = "http://example.com/expected";
            var updatedRedirect = new CustomRedirect(DefaultOldUri.PathAndQuery, updatedUrl);

            _sut.Add(updatedRedirect);

            var actual = _sut.Find(DefaultOldUri);
            Assert.Equal(updatedUrl, actual.NewUrl);
        }

        // Regression tests

        /// <summary>
        /// https://github.com/Geta/404handler/issues/46
        /// </summary>
        [Theory]
        [InlineData("/en/about-us/", "/en/about-us/news-events", "http://localhost:80/en/about-us/news-events-wrong-word",
                    "/en/about-us/news-events/news-events-wrong-word")]
        // https://github.com/Geta/geta-notfoundhandler/issues/137
        [InlineData("/content", "/legacy", "/content/file/", "/legacy/file/")]
        [InlineData("/content", "/legacy/", "/content/file", "/legacy/file")]
        [InlineData("/content", "/legacy/", "/content/file/", "/legacy/file/")]
        [InlineData("/content", "/", "/content/file", "/file")]
        [InlineData("/content", "/legacy", "/content//file", "/legacy/file")]
        // The first hop into a new url nested under the old one still redirects.
        [InlineData("/en/people/", "/en/people/experts", "/en/people/fneksnfa", "/en/people/experts/fneksnfa")]
        [InlineData("/catalog-content", "/catalog-content/true-catalog", "/catalog-content/page", "/catalog-content/true-catalog/page")]
        [InlineData("/catalog-content", "/catalog-content/true-catalog", "/catalog-content/true-catalog-old", "/catalog-content/true-catalog/true-catalog-old")]
        [InlineData("/a", "http://example.com/a/b", "/a/x", "http://example.com/a/b/x")]
        [InlineData("/om-oss", "/om-oss/p%C3%A5ske", "/om-oss/x", "/om-oss/p%C3%A5ske/x")]
        // A path that only looks nested on another host hasn't been redirected yet.
        [InlineData("/a", "https://other.example/a/b", "http://localhost:80/a/b/x", "https://other.example/a/b/b/x")]
        // A new url on the same path only changes the query string, so it isn't nested.
        [InlineData("/page?id=1", "/page?id=2", "/page?id=1&utm=x", "/page?id=1&utm=x&id=2")]
        [InlineData("/a", "/a?x=1", "/a?y=2", "/a?y=2&x=1")]
        // The same host on another port is another site.
        [InlineData("/a", "https://site.example:8443/a/b", "https://site.example/a/b/x", "https://site.example:8443/a/b/b/x")]
        // Nothing to append to a new url of "/" must still redirect there, not to an empty url.
        [InlineData("/content", "/", "/content/content", "/")]
        [InlineData("/content/", "/", "/content", "/")]
        public void Find_should_append_sub_segment(string fromUrl, string toUrl, string notFoundUrl, string expected)
        {
            var redirect = new CustomRedirect(fromUrl, toUrl);
            _sut.Add(redirect);

            var actual = _sut.Find(notFoundUrl.ToUri());

            Assert.Equal(expected, actual.NewUrl);
        }

        /// <summary>
        /// https://github.com/Geta/404handler/issues/90
        /// </summary>
        [Theory]
        [InlineData("/resources/b", "/resources/blog", "http://localhost:80/resources/blog/thisisablog")]
        [InlineData("/product/health", "/product/health-insurance", "http://localhost:80/product/health-insurance-article/what-hipaa-and-how-will-it-affect-your-small-business?q=article/what-hipaa-and-how-will-it-affect-your-small-business")]
        [InlineData("/resources/articles/smal", "/resources/articles/small-business", "http://localhost:80/resources/articles/small-business/understanding-financial-statements")]
        [InlineData("/bar", "/blog", "http://localhost:80/barr")]
        // https://github.com/Geta/404handler/issues/46
        [InlineData("/en/about-us/", "/en/about-us/news-events", "http://localhost:80/en/about-us/news-events/wrong-page")]
        // https://github.com/Geta/geta-notfoundhandler/issues/78
        [InlineData("/en/people/", "/en/people/experts", "http://localhost:80/en/people/experts/fneksnfa")]
        // https://github.com/Geta/geta-notfoundhandler/issues/145
        [InlineData("/catalog-content", "/catalog-content/true-catalog", "http://localhost:80/catalog-content/true-catalog")]
        [InlineData("/catalog-content", "/catalog-content/true-catalog/", "http://localhost:80/catalog-content/true-catalog/page")]
        [InlineData("/", "/home", "http://localhost:80/home/page")]
        [InlineData("/a", "http://localhost/a/b", "http://localhost:80/a/b/x")]
        [InlineData("/a", "https://LOCALHOST/a/b/?q=1", "http://localhost:80/a/b/x")]
        [InlineData("/om-oss", "/om-oss/p%C3%A5ske", "http://localhost:80/om-oss/p%C3%A5ske/x")]
        [InlineData("/om-oss", "http://localhost/om-oss/p%C3%A5ske", "http://localhost:80/om-oss/p%C3%A5ske/x")]
        // An absolute old url only matches exactly, so it can't grow.
        [InlineData("http://localhost/a", "http://localhost/a/b", "http://localhost:80/a/b/x")]
        [InlineData("http://localhost/a", "/a/b", "http://localhost:80/a/b/x")]
        // A new url that only changes the query string has already been applied.
        [InlineData("/page?id=1", "/page?id=2", "http://localhost:80/page?id=1&utm=x&id=2")]
        [InlineData("/a", "/a?x=1", "http://localhost:80/a?y=2&x=1")]
        [InlineData("/a", "/a?x=1&z=2", "http://localhost:80/a?y=2&x=1&z=2")]
        // The same goes for an absolute old url, which is compared to the absolute request.
        [InlineData("http://localhost/a", "/a?x=1", "http://localhost:80/a?y=2&x=1")]
        [InlineData("http://localhost/a", "http://localhost/a?x=1", "http://localhost:80/a?x=1")]
        // A browser doesn't send the fragment of the new url back.
        [InlineData("/a", "/a?x=1#top", "http://localhost:80/a?x=1")]
        [InlineData("/a", "/a/b#top", "http://localhost:80/a/b")]
        // The lookup for legacy values html encodes the request, which must not get past the check.
        [InlineData("/a", "/a/it's", "http://localhost:80/a/it's/x")]
        [InlineData("/a", "/a/b&c", "http://localhost:80/a/b&c/x")]
        [InlineData("/a", "/a?x=1&amp;z=2", "http://localhost:80/a?y=2&x=1&z=2")]
        [InlineData("/a", "//localhost/a/b", "http://localhost:80/a/b/x")]
        public void Find_should_not_cause_redirect_loop(string fromUrl, string toUrl, string notFoundUrl)
        {
            var redirect = new CustomRedirect(fromUrl, toUrl);
            _sut.Add(redirect);

            var actual = _sut.Find(notFoundUrl.ToUri());

            Assert.Null(actual);
        }

        [Theory]
        [InlineData("/a", "/a?x=1", "http://localhost:80/a/?y=2&x=1", null)]
        [InlineData("/a", "/a?x=1", "http://localhost:80/a?y=2", "/a/?y=2&x=1")]
        // Everything under the old url still goes to the new url.
        [InlineData("/a", "/a?x=1", "http://localhost:80/a/c?x=1", "/a/?x=1")]
        [InlineData("/a", "/a/b", "http://localhost:80/a/b/c", "/a/b")]
        public void Find_should_stop_a_skip_append_redirect_growing(string fromUrl, string toUrl, string notFoundUrl, string expected)
        {
            _sut.Add(new CustomRedirect(fromUrl, toUrl, true, RedirectType.Permanent));

            var actual = _sut.Find(notFoundUrl.ToUri());

            Assert.Equal(expected, actual?.NewUrl);
        }

        [Fact]
        public void Find_does_not_fall_through_to_a_broader_redirect_when_under_nested_new_url()
        {
            _sut.Add(new CustomRedirect("/en/people", "/en/people/experts"));
            _sut.Add(new CustomRedirect("/en", "/en/home"));

            var actual = _sut.Find("/en/people/experts/x".ToUri());

            Assert.Null(actual);
        }

        /// <summary>
        /// https://github.com/Geta/404handler/issues/38
        /// </summary>
        [Fact]
        public void Add_allows_similar_culture_specific_urls()
        {
            var first = new CustomRedirect("/digtalpaent", "/");
            _sut.Add(first);
            var second = new CustomRedirect("/digtalpænt", "/");

            var ex = Record.Exception(() => _sut.Add(second));

            Assert.Null(ex);
        }

        /// <summary>
        /// https://github.com/Geta/404handler/issues/36
        /// </summary>
        [Fact]
        public void Find_does_not_add_trailing_slash()
        {
            var expected = "https://domain.mydomain.dk/thisismypdf.pdf";
            var redirect = new CustomRedirect("/something", expected);
            _sut.Add(redirect);

            var actual = _sut.Find(redirect.OldUrl.ToUri());

            Assert.Equal(expected, actual.NewUrl);
        }

        [Fact]
        public void Find_finds_redirect_ignoring_query_string()
        {
            var requestUrl = "/old/?param1=value1&param2=value2";
            var storedUrl = "/old/";
            var redirect = new CustomRedirect(storedUrl, DefaultNewUri.PathAndQuery);
            _sut.Add(redirect);

            var actual = _sut.Find(requestUrl.ToUri());

            Assert.NotNull(actual);
        }

        [Fact]
        public void Find_finds_redirect_with_sub_segment_ignoring_query_string()
        {
            var requestUrl = "/old/segment/?param1=value1&param2=value2";
            var storedUrl = "/old/";
            var redirect = new CustomRedirect(storedUrl, DefaultNewUri.PathAndQuery);
            _sut.Add(redirect);

            var actual = _sut.Find(requestUrl.ToUri());

            Assert.NotNull(actual);
        }

        private void WithProvider(string oldUrl, string newUrl)
        {
            var provider = A.Fake<INotFoundHandler>();
            A.CallTo(() => provider.RewriteUrl(A<string>._)).Returns(RewriteResult.Empty);
            var rewriteResult = string.IsNullOrEmpty(newUrl)
                ? RewriteResult.Empty
                : new RewriteResult(newUrl, RedirectType.Temporary);
            A.CallTo(() => provider.RewriteUrl(oldUrl)).Returns(rewriteResult);
            _providers.Add(provider);
        }

        /// <summary>
        /// https://github.com/Geta/404handler/issues/117
        /// </summary>
        [Fact]
        public void Find_should_not_throw_on_query_string_prefix_for_same_segment()
        {
            var requestUrl = "/old/?param1=value1&param2=value2";
            var storedUrl = "/old/?param1=value1";
            var redirect = new CustomRedirect(storedUrl, DefaultNewUri.PathAndQuery);
            _sut.Add(redirect);

            var actual = _sut.Find(requestUrl.ToUri());

            Assert.NotNull(actual);
        }

        [Fact]
        public void Find_should_find_redirect_with_special_chars()
        {
            var requestUrl = "/old/noël";
            var newUrl = "/new/noël";
            var redirect = new CustomRedirect(requestUrl, newUrl);
            _sut.Add(redirect);

            var actual = _sut.Find(requestUrl.ToUri());

            Assert.Equal(redirect.NewUrl, actual.NewUrl);
        }

        /// <summary>
        /// https://github.com/Geta/404handler/issues/118
        /// </summary>
        [Fact]
        public void Find_does_not_give_nullreferenceexception_with_exact_deleted_hit()
        {
            var storedUrl = "/contentassets";
            var requesttUrl = "/contentassets";
            var redirect = new CustomRedirect(storedUrl, RedirectState.Deleted);
            _sut.Add(redirect);

            var actual = _sut.Find(requesttUrl.ToUri());

            Assert.NotNull(actual);
        }

        /// <summary>
        /// https://github.com/Geta/geta-notfoundhandler/issues/176
        /// </summary>
        [Theory]
        [InlineData("/contentassets/")]
        [InlineData("/contentassets?a=1")]
        [InlineData("/contentassets/moreurl")]
        public void Find_keeps_deleted_state_with_partial_deleted_hit(string requestUrl)
        {
            _sut.Add(new CustomRedirect("/contentassets", RedirectState.Deleted));

            var actual = _sut.Find(requestUrl.ToUri());

            Assert.Equal((int)RedirectState.Deleted, actual.State);
        }

        /// <summary>
        /// https://github.com/Geta/404handler/issues/171
        /// </summary>
        [Fact]
        public void Find_does_not_add_slash_when_querystring_parameter()
        {
            var collection = new CustomRedirectCollection
            {
                new CustomRedirect("/content/file", "/legacy"),
                new CustomRedirect("/content/file/", "/legacy/")
            };

            var urlToFind = "/content/file?doc=filename";
            var expected = "/legacy?doc=filename";

            var actual = collection.Find(urlToFind.ToUri());

            Assert.Equal(expected, actual.NewUrl);
        }

        /// <summary>
        /// https://github.com/Geta/404handler/issues/171
        /// </summary>
        [Fact]
        public void Find_does_keep_slash_when_querystring_parameter()
        {
            var collection = new CustomRedirectCollection
            {
                new CustomRedirect("/content/file", "/legacy"),
                new CustomRedirect("/content/file/", "/legacy/")
            };

            var urlToFind = "/content/file/?doc=filename";
            var expected = "/legacy/?doc=filename";

            var actual = collection.Find(urlToFind.ToUri());

            Assert.Equal(expected, actual.NewUrl);
        }

        /// <summary>
        /// https://github.com/Geta/404handler/issues/171
        /// </summary>
        [Fact]
        public void Find_does_not_add_slash_when_querystring_parameters()
        {
            var collection = new CustomRedirectCollection
            {
                new CustomRedirect("/content/file", "/legacy"),
                new CustomRedirect("/content/file/", "/legacy/")
            };

            var urlToFind = "/content/file?doc=filename&param2=value";
            var expected = "/legacy?doc=filename&param2=value";

            var actual = collection.Find(urlToFind.ToUri());

            Assert.Equal(expected, actual.NewUrl);
        }

        /// <summary>
        /// https://github.com/Geta/geta-notfoundhandler/issues/54
        /// </summary>
        [Fact]
        public void Find_decodes_plus_sign_correctly()
        {
            var collection = new CustomRedirectCollection
            {
                new("/search/ps_90/s_13?kw=oakley+radar+ev", "/search-page/?query=oakley+radar+ev")
            };

            var urlToFind = "/search/ps_90/s_13?kw=oakley+radar+ev";
            var expected = "/search-page/?query=oakley+radar+ev";

            var actual = collection.Find(urlToFind.ToUri());

            Assert.Equal(expected, actual.NewUrl); 
        }
        
        [Theory]
        [InlineData("/content//evil.com", "/evil.com")]
        [InlineData("/content/%2Fevil.com", "/evil.com")]
        [InlineData("/content/%5Cevil.com", "/evil.com")]
        public void Find_does_not_build_protocol_relative_url_from_request_path(string notFoundUrl, string expected)
        {
            _sut.Add(new CustomRedirect("/content", "/"));

            var actual = _sut.Find(notFoundUrl.ToUri());

            Assert.Equal(expected, actual.NewUrl);
        }

        [Theory]
        [InlineData("/content/", "/legacy", "/content//content", "/legacy")]
        [InlineData("/content/", "/legacy", "/content//content/file", "/legacy/file")]
        [InlineData("/", "/home", "/?param=value", "/home?param=value")]
        [InlineData("/content//", "/legacy", "/content///content", "/legacy")]
        [InlineData("/content/", "/legacy", "/content/content/file", "/legacy/file")]
        public void Find_does_not_throw_when_sub_segment_repeats_old_url(
            string fromUrl,
            string toUrl,
            string notFoundUrl,
            string expected)
        {
            _sut.Add(new CustomRedirect(fromUrl, toUrl));

            var actual = _sut.Find(notFoundUrl.ToUri());

            Assert.Equal(expected, actual.NewUrl);
        }
    }
}
