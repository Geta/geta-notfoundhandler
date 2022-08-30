// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Geta.NotFoundHandler.Tests.Hosting;
using Xunit;
using System.Net;

namespace Geta.NotFoundHandler.Tests
{
    public class ExternalHandlerTests
    {
        [Fact]
        public async Task Request_does_not_build_forever()
        {
            var builder = new RedirectServerBuilder();

            builder.AddRedirect("/catalog-content", "/catalog-content/catalog-content");
            builder.AddRedirect("/catalog-content/nice-sweater", "/catalog-content");
            builder.AddRedirect("/catalog-content/redirect-by-code", "/catalog-content/nice-sweater");

            using var server = builder.Build();
            using var client = server.CreateClient();

            var response = await client.GetAsync("/catalog-content/redirect-by-code");

            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);

            var location = response.Headers.Location?.ToString();
            Assert.Equal("/catalog-content/nice-sweater", location);

            var iterations = 0;

            while (response.StatusCode == HttpStatusCode.MovedPermanently)
            {
                response = await client.GetAsync(location);
                location = response.Headers.Location?.ToString();

                Assert.True(++iterations < 100);
            }
        }

        [Fact]
        public async Task Request_doesnt_loop()
        {
            var builder = new RedirectServerBuilder();

            builder.AddRedirect("https://localhost/a/b", "/");
            builder.AddRedirect("/a/b?a=b", "/");

            using var server = builder.Build();
            using var client = server.CreateClient();

            var response = await client.GetAsync("https://localhost/a/b?a=b");

            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);

            var location = response.Headers.Location?.ToString();
            Assert.Equal("/?a=b", location);
        }
    }
}
