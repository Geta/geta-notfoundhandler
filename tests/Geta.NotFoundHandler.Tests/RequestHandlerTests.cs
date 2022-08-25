using System;
using System.Net;
using FakeItEasy;
using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Core.Suggestions;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Geta.NotFoundHandler.Tests.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Geta.NotFoundHandler.Tests
{
    public class RequestHandlerTests
    {
        private readonly IRedirectHandler _redirectHandler;
        private readonly IRequestLogger _requestLogger;
        private readonly HttpContext _httpContext;
        private readonly RequestHandler _sut;
        private static readonly Uri DefaultOldUri = new Uri("http://example.com/old");
        private readonly NotFoundHandlerOptions _configuration;

        public RequestHandlerTests()
        {
            _redirectHandler = A.Fake<IRedirectHandler>();
            _requestLogger = A.Fake<IRequestLogger>();
            var options = A.Fake<IOptions<NotFoundHandlerOptions>>();
            _configuration = new NotFoundHandlerOptions();
            A.CallTo(() => options.Value).Returns(_configuration);
            var logger = A.Fake<ILogger<RequestHandler>>();
            _sut = A.Fake<RequestHandler>(
                o => o.WithArgumentsForConstructor(new object[] { _redirectHandler, _requestLogger, options, logger })
                    .CallsBaseMethods());

            _httpContext = new DefaultHttpContext();
            _httpContext.Response.StatusCode = 404;
            _httpContext.Request.Scheme = "https";
            _httpContext.Request.Host = new HostString("localhost");

            WhenIsRemote();
            WhenIsNotResourceFile();
        }

        [Fact]
        public void Handle_returns_when_response_is_not_404()
        {
            _httpContext.Response.StatusCode = 400;

            _sut.Handle(_httpContext);

            AssertNotHandled(_httpContext);
        }

        [Fact]
        public void Handle_returns_when_is_localhost()
        {
            WhenIsLocalhost();

            _sut.Handle(_httpContext);

            AssertNotHandled(_httpContext);
        }

        [Fact]
        public void Handle_returns_when_is_off()
        {
            WhenIsOff();

            _sut.Handle(_httpContext);

            AssertNotHandled(_httpContext);
        }

        [Fact]
        public void Handle_returns_when_is_resource_file()
        {
            WhenIsResourceFile();

            _sut.Handle(_httpContext);

            AssertNotHandled(_httpContext);
        }

        [Fact]
        public void Handle_sets_404_response_when_unable_redirect()
        {
            WhenRedirectRequestNotHandled();

            _sut.Handle(_httpContext);

            Assert404ResponseSet(_httpContext);
        }

        [Fact]
        public void Handle_sets_410_response_when_resource_deleted()
        {
            WhenResourceDeleted();

            _sut.Handle(_httpContext);

            Assert410ResponseSet(_httpContext);
        }

        [Fact]
        public void Handle_redirects_when_redirect_url_found()
        {
            var redirect = new CustomRedirect("http://example.com/missing", RedirectState.Saved)
            {
                NewUrl = "http://example.com/page"
            };
            WhenRedirectUrlFound(redirect);

            _sut.Handle(_httpContext);

            AssertRedirected(_httpContext, redirect);
        }

        [Fact]
        public void Handle_handles_request_only_once()
        {
            WhenRedirectRequestNotHandled();

            _sut.Handle(_httpContext);
            _sut.Handle(_httpContext);

            AssertRequestHandledOnce();
        }

        /// <summary>
        /// Contributed by https://github.com/AndersHGP in PR #34
        /// </summary>
        [Fact]
        public void Handle_redirects_when_redirect_url_found_with_non_ascii_characters()
        {
            var redirect = new CustomRedirect("http://example.com/missing", RedirectState.Saved)
            {
                NewUrl = "http://example.com/page/ö"
            };

            WhenRedirectUrlFound(redirect);

            _sut.Handle(_httpContext);

            AssertRedirected(_httpContext, redirect);
        }

        [Fact]
        public void HandleRequest_returns_false_when_redirect_not_found()
        {
            WhenRedirectNotFound();

            var actual = _sut.HandleRequest(DefaultOldUri, new Uri("http://example.com/path"), out var _);

            Assert.False(actual);
        }

        [Fact]
        public void HandleRequest_logs_request_when_redirect_not_found_and_logging_is_on()
        {
            WhenRedirectNotFound();
            WhenLoggingIsOn();
            var referrer = "http://example.com/home".ToUri();
            var urlNotFound = "http://example.com/path".ToUri();

            _sut.HandleRequest(referrer, urlNotFound, out var _);

            AssertRequestLogged(referrer, urlNotFound);
        }

        [Fact]
        public void HandleRequest_doesnt_log_request_when_redirect_not_found_and_logging_is_off()
        {
            WhenRedirectNotFound();
            WhenLoggingIsOff();
            var referrer = "http://example.com/home".ToUri();
            var urlNotFound = "http://example.com/path".ToUri();

            _sut.HandleRequest(referrer, urlNotFound, out var _);

            AssertRequestNotLogged(referrer, urlNotFound);
        }

        [Fact]
        public void HandleRequest_returns_true_when_redirect_found_with_deleted_state()
        {
            var redirect = new CustomRedirect("http://example.com/missing", RedirectState.Deleted);
            WhenRedirectFound(redirect);

            var actual = _sut.HandleRequest(DefaultOldUri, new Uri("http://example.com/path"), out var _);

            Assert.True(actual);
        }

        [Fact]
        public void HandleRequest_returns_redirect_when_redirect_found_with_deleted_state()
        {
            var redirect = new CustomRedirect("http://example.com/missing", RedirectState.Deleted);
            WhenRedirectFound(redirect);

            _sut.HandleRequest(DefaultOldUri, new Uri("http://example.com/path"), out var actual);

            Assert.Equal(redirect.OldUrl, actual.OldUrl);
        }

        [Fact]
        public void HandleRequest_returns_true_when_redirect_found_with_saved_state()
        {
            var redirect = new CustomRedirect("http://example.com/found", RedirectState.Saved);
            WhenRedirectFound(redirect);

            var actual = _sut.HandleRequest(DefaultOldUri, new Uri("http://example.com/path"), out var _);

            Assert.True(actual);
        }

        [Fact]
        public void HandleRequest_returns_redirect_when_redirect_found_with_saved_state()
        {
            var redirect = new CustomRedirect("http://example.com/found", RedirectState.Saved);
            WhenRedirectFound(redirect);

            _sut.HandleRequest(DefaultOldUri, new Uri("http://example.com/path"), out var actual);

            Assert.Equal(redirect.OldUrl, actual.OldUrl);
        }

        [Fact]
        public void HandleRequest_returns_false_when_redirect_is_same_as_not_found()
        {
            var sameUri = new Uri("http://example.com/same");
            var redirect = new CustomRedirect(sameUri.ToString(), RedirectState.Saved)
            {
                NewUrl = sameUri.PathAndQuery
            };
            WhenRedirectFound(redirect);

            var actual = _sut.HandleRequest(DefaultOldUri, sameUri, out var _);

            Assert.False(actual);
        }

        private void WhenRedirectFound(CustomRedirect redirect)
        {
            A.CallTo(() => _redirectHandler.Find(A<Uri>._)).Returns(redirect);
        }

        private void AssertRequestNotLogged(Uri referrer, Uri urlNotFound)
        {
            A.CallTo(() => _requestLogger.LogRequest(urlNotFound.PathAndQuery, referrer.ToString()))
                .MustNotHaveHappened();
        }

        private void AssertRequestLogged(Uri referrer, Uri urlNotFound)
        {
            A.CallTo(() => _requestLogger.LogRequest(urlNotFound.PathAndQuery, referrer.ToString()))
                .MustHaveHappened();
        }

        private void WhenLoggingIsOn()
        {
            _configuration.Logging = LoggerMode.On;
        }

        private void WhenLoggingIsOff()
        {
            _configuration.Logging = LoggerMode.Off;
        }

        private void WhenRedirectNotFound()
        {
            A.CallTo(() => _redirectHandler.Find(A<Uri>._)).Returns(null);
        }

        private void AssertRequestHandledOnce()
        {
            CustomRedirect _;
            A.CallTo(() => _sut.HandleRequest(A<Uri>._, A<Uri>._, out _)).MustHaveHappened(1, Times.Exactly);
        }

        private void WhenRedirectUrlFound(CustomRedirect redirect)
        {
            A.CallTo(() => _sut.HandleRequest(A<Uri>._, A<Uri>._, out redirect)).Returns(true);
        }

        private void WhenResourceDeleted()
        {
            var redirect = new CustomRedirect("http://example.com", RedirectState.Deleted);
            A.CallTo(() => _sut.HandleRequest(A<Uri>._, A<Uri>._, out redirect)).Returns(true);
        }

        private void WhenRedirectRequestNotHandled()
        {
            CustomRedirect outRedirect;
            A.CallTo(() => _sut.HandleRequest(A<Uri>._, A<Uri>._, out outRedirect)).Returns(false);
        }

        private void WhenIsResourceFile()
        {
            A.CallTo(() => _sut.IsResourceFile(A<Uri>._)).Returns(true);
        }

        private void WhenIsNotResourceFile()
        {
            A.CallTo(() => _sut.IsResourceFile(A<Uri>._)).Returns(false);
        }

        private void WhenIsLocalhost()
        {
            // Set local
            var sameIp = new IPAddress(123);
            _httpContext.Connection.RemoteIpAddress = sameIp;
            _httpContext.Connection.LocalIpAddress = sameIp;

            _configuration.HandlerMode = FileNotFoundMode.RemoteOnly;
        }

        private void WhenIsRemote()
        {
            SetRemote();
            _configuration.HandlerMode = FileNotFoundMode.On;
        }

        private void WhenIsOff()
        {
            SetRemote();
            _configuration.HandlerMode = FileNotFoundMode.Off;
        }

        private void SetRemote()
        {
            // Set remote
            _httpContext.Connection.RemoteIpAddress = new IPAddress(123);
            _httpContext.Connection.LocalIpAddress = new IPAddress(321);
        }

        private static void AssertRedirected(HttpContext context, CustomRedirect redirect)
        {
            Assert.True(RequestHandler.IsHandled(context));
            Assert.NotEqual(404, context.Response.StatusCode);

            var headers = context.Response.Headers;
            var redirectLocation = headers.ContainsKey("Location") ? (string)headers["Location"] : string.Empty;

            if (Uri.TryCreate(redirect.NewUrl, UriKind.RelativeOrAbsolute, out var uri))
            {
                redirect.NewUrl = UriHelper.Encode(uri);
            }

            Assert.Equal(redirect.NewUrl, redirectLocation);
        }

        private static void Assert404ResponseSet(HttpContext context)
        {
            Assert.True(RequestHandler.IsHandled(context));
            Assert.Equal(404, context.Response.StatusCode);
        }

        private static void Assert410ResponseSet(HttpContext context)
        {
            Assert.True(RequestHandler.IsHandled(context));
            Assert.Equal(410, context.Response.StatusCode);
        }

        private static void AssertNotHandled(HttpContext context)
        {
            Assert.False(RequestHandler.IsHandled(context));
        }
    }
}
