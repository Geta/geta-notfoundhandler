// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
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
        private Exception _logFailure;

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
                _ => Task.CompletedTask,
                activeStatusCodes: new[] { StatusCodes.Status200OK });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Re_executed_status_code_page_redirects_using_original_url()
        {
            var response = await GetAsync("/old/sub?a=1", NotFoundOrErrorPage, Saved("/old", "/new"), inner: ReExecute);

            Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
            Assert.Equal("/new/sub?a=1", response.Headers.Location?.ToString());
        }

        [Fact]
        public async Task Redirects_without_rendering_status_code_page_registered_before_handler()
        {
            var paths = new List<string>();

            var response = await GetAsync("/old", Recording(paths), Saved("/old", "/new"), outer: ReExecute);

            Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
            Assert.Equal("/new", response.Headers.Location?.ToString());
            Assert.Equal(new[] { "/old" }, paths);
        }

        [Fact]
        public async Task Shows_410_status_code_page_for_deleted_url()
        {
            var paths = new List<string>();

            var response = await GetAsync("/gone", Recording(paths), Deleted("/gone"), outer: ReExecute);

            Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
            Assert.Equal(new[] { "/gone", "/error/410" }, paths);
        }

        [Fact]
        public async Task Keeps_custom_page_when_handling_fails_as_response_starts()
        {
            _logFailure = new InvalidOperationException("Logging failed");

            var response = await GetAsync("/missing", WriteNotFoundPage);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(CustomPage, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Does_not_fail_when_response_started_before_handler()
        {
            var response = await GetAsync(
                "/old",
                _ => Task.CompletedTask,
                Saved("/old", "/new"),
                outer: app => app.Use(async (context, next) =>
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.Response.StartAsync();
                    await next();
                }));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Null(response.Headers.Location);
        }

        [Fact]
        public async Task Re_executed_status_code_page_logs_original_url_once()
        {
            var response = await GetAsync("/missing", NotFoundOrErrorPage, inner: ReExecute);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(CustomPage, await response.Content.ReadAsStringAsync());
            Assert.Equal(new[] { "/missing" }, _loggedUrls);
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

        private static RequestDelegate Recording(List<string> paths)
        {
            return context =>
            {
                paths.Add(context.Request.Path);
                return NotFoundOrErrorPage(context);
            };
        }

        private static void ReExecute(IApplicationBuilder app)
        {
            app.UseStatusCodePagesWithReExecute("/error/{0}");
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
            Action<IApplicationBuilder> outer = null,
            Action<IApplicationBuilder> inner = null,
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
                .Invokes((string loggedUrl, string _) =>
                {
                    if (_logFailure != null)
                    {
                        throw _logFailure;
                    }

                    _loggedUrls.Add(loggedUrl);
                });

            var options = new NotFoundHandlerOptions();
            if (activeStatusCodes != null)
            {
                options.ActiveStatusCodes = activeStatusCodes;
            }

            var requestHandler = new RequestHandler(
                redirectHandler,
                requestLogger,
                Options.Create(options),
                NullLogger<RequestHandler>.Instance,
                new RedirectLoopGuard(Options.Create(options)));

            var builder = new TestServerBuilder();
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(requestHandler);
            });
            builder.Configure(app =>
            {
                outer?.Invoke(app);
                app.UseMiddleware<NotFoundHandlerMiddleware>();
                inner?.Invoke(app);
                app.Run(application);
            });

            using var server = builder.Build();
            using var client = server.CreateClient();

            return await client.GetAsync(url);
        }
    }
}
