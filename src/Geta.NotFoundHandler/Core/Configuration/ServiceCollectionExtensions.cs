// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using Geta.NotFoundHandler.Core.CustomRedirects;
using Geta.NotFoundHandler.Core.Data;
using Geta.NotFoundHandler.Core.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Geta.NotFoundHandler.Core.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNotFoundHandler(this IServiceCollection services)
        {
            return AddNotFoundHandler(services, o => { });
        }

        public static IServiceCollection AddNotFoundHandler(
            this IServiceCollection services,
            Action<NotFoundHandlerOptions> setupAction)
        {
            services.AddTransient<DataAccessBaseEx>();
            services.AddSingleton<IRequestLogger>(RequestLogger.Instance);
            services.AddTransient<IRedirectHandler>(_ => CustomRedirectHandler.Current); // Load per-request as it is read from the cache
            services.AddTransient<RequestHandler>();

            services.AddTransient<IRedirectsService, DefaultRedirectsService>();
            services.AddTransient<IRepository<CustomRedirect>, SqlRedirectRepository>();
            services.AddTransient<IRedirectLoader, SqlRedirectRepository>();

            services.AddOptions<NotFoundHandlerOptions>().Configure<IConfiguration>((options, configuration) =>
            {
                setupAction(options);
                foreach (var provider in options.Providers)
                {
                    services.AddSingleton(typeof(INotFoundHandler), provider);
                }

                configuration.GetSection("Geta:NotFoundHandler").Bind(options);
            });

            return services;
        }
    }
}