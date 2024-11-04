// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Linq;
using EPiServer.Shell.Modules;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Geta.NotFoundHandler.Optimizely.Core.Events;
using Geta.NotFoundHandler.Optimizely.Data;
using Geta.NotFoundHandler.Optimizely.Infrastructure.Initialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Geta.NotFoundHandler.Optimizely.Infrastructure.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOptimizelyNotFoundHandler(this IServiceCollection services)
        {
            return AddOptimizelyNotFoundHandler(services, _ => { });
        }

        public static IServiceCollection AddOptimizelyNotFoundHandler(
            this IServiceCollection services,
            Action<OptimizelyNotFoundHandlerOptions> setupAction)
        {
            services.AddSingleton<OptimizelySyncEvents>();
            services.AddSingleton<ContentUrlHistoryEvents>();
            services.AddTransient<Upgrader>();
            services.AddSingleton<ContentLinkLoader>();
            services.AddSingleton<ContentKeyGenerator>();
            services.AddSingleton<Func<ContentKeyGenerator>>(x => x.GetService<ContentKeyGenerator>);
            services.AddSingleton<ContentUrlLoader>();
            services.AddSingleton<SqlContentUrlHistoryRepository>();
            services.AddSingleton<IRepository<ContentUrlHistory>, SqlContentUrlHistoryRepository>();
            services.AddSingleton<IContentUrlHistoryLoader, SqlContentUrlHistoryRepository>();
            services.AddSingleton<ContentUrlIndexer>();
            services.AddSingleton<Func<ContentUrlIndexer>>(x => x.GetService<ContentUrlIndexer>);
            services.AddSingleton<RedirectBuilder>();

            // Background service
            services.AddSingleton<IAutomaticRedirectsService, DefaultAutomaticRedirectsService>();
            services.AddSingleton<ChannelMovedContentRegistratorQueue>();
            services.AddSingleton<IMovedContentRegistratorQueue, ChannelMovedContentRegistratorQueue>();
            services.AddHostedService<MovedContentRegistratorBackgroundService>();

            var providerOptions = new OptimizelyNotFoundHandlerOptions();
            setupAction(providerOptions);
            foreach (var provider in providerOptions.ContentKeyProviders)
            {
                services.AddSingleton(typeof(IContentKeyProvider), provider);
            }

            foreach (var provider in providerOptions.ContentLinkProviders)
            {
                services.AddSingleton(typeof(IContentLinkProvider), provider);
            }

            foreach (var provider in providerOptions.ContentUrlProviders)
            {
                services.AddSingleton(typeof(IContentUrlProvider), provider);
            }

            services.Configure<ProtectedModuleOptions>(
                pm =>
                {
                    if (!pm.Items.Any(i => i.Name.Equals(Constants.ModuleName, StringComparison.OrdinalIgnoreCase)))
                    {
                        pm.Items.Add(new ModuleDetails { Name = Constants.ModuleName });
                    }
                });

            services.AddOptions<OptimizelyNotFoundHandlerOptions>().Configure<IConfiguration>((options, configuration) =>
            {
                setupAction(options);
                configuration.GetSection(OptimizelyNotFoundHandlerOptions.Section).Bind(options);
            });

            return services;
        }
    }
}
