// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Core.Suggestions;
using Geta.NotFoundHandler.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Geta.NotFoundHandler.Infrastructure.Configuration
{
    public static class ServiceCollectionExtensions
    {
        private static readonly Action<AuthorizationPolicyBuilder> DefaultPolicy = p => p.RequireRole("Administrators");

        public static IServiceCollection AddNotFoundHandler(this IServiceCollection services)
        {
            return AddNotFoundHandler(services, o => { }, DefaultPolicy);
        }

        public static IServiceCollection AddNotFoundHandler(
            this IServiceCollection services,
            Action<NotFoundHandlerOptions> setupAction)
        {
            return AddNotFoundHandler(services, o => { }, DefaultPolicy);
        }

        public static IServiceCollection AddNotFoundHandler(
            this IServiceCollection services,
            Action<NotFoundHandlerOptions> setupAction,
            Action<AuthorizationPolicyBuilder> configurePolicy)
        {
            services.AddTransient<DataAccessBaseEx>();
            services.AddSingleton<IRequestLogger>(RequestLogger.Instance);
            services.AddTransient<IRedirectHandler>(_ => CustomRedirectHandler
                                                        .Current); // Load per-request as it is read from the cache
            services.AddTransient<RequestHandler>();

            services.AddTransient<IRedirectsService, DefaultRedirectsService>();
            services.AddTransient<IRepository<CustomRedirect>, SqlRedirectRepository>();
            services.AddTransient<IRedirectLoader, SqlRedirectRepository>();

            services.AddTransient<ISuggestionService, DefaultSuggestionService>();
            services.AddTransient<IRepository<Suggestion>, SqlSuggestionRepository>();
            services.AddTransient<ISuggestionLoader, SqlSuggestionRepository>();

            var providerOptions = new NotFoundHandlerOptions();
            setupAction(providerOptions);
            foreach (var provider in providerOptions.Providers)
            {
                services.AddSingleton(typeof(INotFoundHandler), provider);
            }

            services.AddOptions<NotFoundHandlerOptions>().Configure<IConfiguration>((options, configuration) =>
            {
                setupAction(options);
                configuration.GetSection("Geta:NotFoundHandler").Bind(options);
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(Constants.PolicyName, configurePolicy);
            });

            return services;
        }
    }
}
