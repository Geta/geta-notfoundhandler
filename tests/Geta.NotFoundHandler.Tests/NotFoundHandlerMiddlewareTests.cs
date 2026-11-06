// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FakeItEasy;
using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Core.Suggestions;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Geta.NotFoundHandler.Infrastructure.Initialization;
using Geta.NotFoundHandler.Tests.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Geta.NotFoundHandler.Tests
{
    public class NotFoundHandlerMiddlewareTests
    {
        private const string CustomPage = "<h1>custom page</h1>";

        private readonly List<string> _loggedUrls = new();

        [Fact]
        public async Task Redirects_when_application_already_wrote_response_body()
        {
            var response = await GetAsync("/old", WriteNotFoundPage, Saved("/old", "/new"));

            Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
            Assert.Equal("/new", response.Headers.Location?.ToString());
        }

        [Fact]
        public async Task Returns_410_for_deleted_url_when_response_is_empty()
        {
            var response = await GetAsync("/gone", NotFoundWithoutBody, Deleted("/gone"));

            Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        }

        [Fact]
        public async Task Returns_410_and_keeps_custom_page_for_deleted_url()
        {
            var response = await GetAsync("/gone", WriteNotFoundPage, Deleted("/gone"));

            Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
            Assert.Equal(CustomPage, await response.Content.ReadAsStringAsync());
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Keeps_status_and_custom_page_when_no_redirect_found()
        {
            var response = await GetAsync("/missing", WriteNotFoundPage);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(CustomPage, await response.Content.ReadAsStringAsync());
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(new[] { "/missing" }, _loggedUrls);
        }

        [Fact]
        public async Task Keeps_active_status_code_when_no_redirect_found()
        {
            var response = await GetAsync(
                "/page",
                context =>
                {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    return Task.CompletedTask;
                },
                activeStatusCodes: new[] { StatusCodes.Status200OK });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Does_not_handle_status_code_that_is_not_active()
        {
            var response = await GetAsync(
                "/old",
                context =>
                {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    return Task.CompletedTask;
                },
                redirects: Saved("/old", "/new"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Null(response.Headers.Location);
        }

        [Fact]
        public async Task Re_executed_status_code_page_redirects_using_original_url()
        {
            var response = await GetAsync("/old/sub?a=1", NotFoundOrErrorPage, Saved("/old", "/new"), reExecute: true);

            Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
            Assert.Equal("/new/sub?a=1", response.Headers.Location?.ToString());
        }

        [Fact]
        public async Task Re_executed_status_code_page_logs_original_url_once()
        {
            var response = await GetAsync("/missing", NotFoundOrErrorPage, reExecute: true);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(CustomPage, await response.Content.ReadAsStringAsync());
            Assert.Equal(new[] { "/missing" }, _loggedUrls);
        }

        [Fact]
        public async Task Re_executed_status_code_page_returns_410_for_deleted_url()
        {
            var response = await GetAsync("/gone", NotFoundOrErrorPage, Deleted("/gone"), reExecute: true);

            Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
            Assert.Equal(CustomPage, await response.Content.ReadAsStringAsync());
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        }

        private static Task NotFoundWithoutBody(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        }

        private static async Task WriteNotFoundPage(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await WritePage(context);
        }

        private static Task NotFoundOrErrorPage(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments("/error") ? WritePage(context) : NotFoundWithoutBody(context);
        }

        private static async Task WritePage(HttpContext context)
        {
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(CustomPage);
        }

        private static CustomRedirect Saved(string oldUrl, string newUrl)
        {
            return new CustomRedirect(oldUrl, newUrl, false, RedirectType.Permanent);
        }

        private static CustomRedirect Deleted(string oldUrl)
        {
            return new CustomRedirect(oldUrl, RedirectState.Deleted);
        }

        private async Task<HttpResponseMessage> GetAsync(
            string url,
            RequestDelegate application,
            CustomRedirect redirects = null,
            bool reExecute = false,
            int[] activeStatusCodes = null)
        {
            var collection = new CustomRedirectCollection();
            if (redirects != null)
            {
                collection.Add(redirects);
            }

            var redirectHandler = new CustomRedirectHandler();
            redirectHandler.Set(collection);

            var requestLogger = A.Fake<IRequestLogger>();
            A.CallTo(() => requestLogger.ShouldLogRequest(A<string>._)).Returns(true);
            A.CallTo(() => requestLogger.LogRequest(A<string>._, A<string>._))
                .Invokes((string loggedUrl, string _) => _loggedUrls.Add(loggedUrl));

            var options = new NotFoundHandlerOptions();
            if (activeStatusCodes != null)
            {
                options.ActiveStatusCodes = activeStatusCodes;
            }

            var requestHandler = new RequestHandler(
                redirectHandler,
                requestLogger,
                Options.Create(options),
                NullLogger<RequestHandler>.Instance);

            var builder = new TestServerBuilder();
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(requestHandler);
                services.AddRouting();
            });
            builder.Configure(app =>
            {
                if (reExecute)
                {
                    app.UseStatusCodePagesWithReExecute("/error/{0}");
                }

                app.UseMiddleware<NotFoundHandlerMiddleware>();
                app.Run(application);
            });

            using var server = builder.Build();
            using var client = server.CreateClient();

            var response = await client.GetAsync(url);
            await response.Content.LoadIntoBufferAsync();

            return response;
        }
    }
}
