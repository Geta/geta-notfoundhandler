using System.Collections.Generic;
using System;
using FakeItEasy;
using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Core.Suggestions;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Geta.NotFoundHandler.Infrastructure.Initialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Tests.Hosting
{
    public class RedirectServerBuilder : TestServerBuilder
    {
        private readonly CustomRedirectCollection _redirectCollection;
        private readonly IList<Action<IEndpointRouteBuilder>> _endpointActions;

        public RedirectServerBuilder()
        {
            _redirectCollection = new CustomRedirectCollection();
            _endpointActions = new List<Action<IEndpointRouteBuilder>>();
        }

        public virtual void AddRedirect(CustomRedirect customRedirect)
        {
            _redirectCollection.Add(customRedirect);
        }

        public virtual void AddRedirect(string oldUrl, string newUrl, bool skipWildCardAppend = false, RedirectType redirectType = RedirectType.Permanent)
        {
            var redirect = new CustomRedirect(oldUrl, newUrl, skipWildCardAppend, redirectType);
            AddRedirect(redirect);
        }

        public virtual void AddEndpoints(Action<IEndpointRouteBuilder> action)
        {
            _endpointActions.Add(action);
        }

        public override TestServer Build()
        {
            var redirectHandler = new CustomRedirectHandler();

            redirectHandler.Set(_redirectCollection);

            var requestLogger = A.Fake<IRequestLogger>();
            var logger = NullLogger<RequestHandler>.Instance;
            var options = Options.Create(new NotFoundHandlerOptions());

            var requestHandler = new RequestHandler(redirectHandler, requestLogger, options, logger);

            ConfigureServices(services =>
            {
                services.AddSingleton(requestHandler);
                services.AddRouting();
            });

            Configure(app =>
            {
                app.UseRouting();
                app.UseMiddleware<NotFoundHandlerMiddleware>();
                app.UseEndpoints(endpoints =>
                {
                    foreach (var action in _endpointActions)
                    {
                        action(endpoints);
                    }
                });
            });

            return base.Build();
        }
    }
}
