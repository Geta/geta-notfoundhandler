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
            services.AddTransient<ContentLinkLoader>();
            services.AddTransient<ContentKeyGenerator>();
            services.AddTransient<ContentUrlLoader>();
            services.AddTransient<IContentKeyProvider, CmsContentKeyProvider>();
            services.AddTransient<IContentLinkProvider, CmsContentLinkProvider>();
            services.AddTransient<IContentUrlProvider, CmsContentUrlProvider>();
            services.AddTransient<SqlContentUrlHistoryRepository>();
            services.AddTransient<IRepository<ContentUrlHistory>>(x => x.GetRequiredService<SqlContentUrlHistoryRepository>());
            services.AddTransient<IContentUrlHistoryLoader>(x => x.GetRequiredService<SqlContentUrlHistoryRepository>());
            services.AddSingleton<Func<ContentUrlIndexer>>(x => x.GetService<ContentUrlIndexer>);
            services.AddTransient<ContentUrlIndexer>();
            services.AddTransient<RedirectBuilder>();

            services.Configure<ProtectedModuleOptions>(
                pm =>
                {
                    if (!pm.Items.Any(i => i.Name.Equals(Constants.ModuleName, StringComparison.OrdinalIgnoreCase)))
                    {
                        pm.Items.Add(new ModuleDetails { Name = Constants.ModuleName });
                    }
                });

            var providerOptions = new OptimizelyNotFoundHandlerOptions();
            setupAction(providerOptions);
            services.AddOptions<OptimizelyNotFoundHandlerOptions>().Configure<IConfiguration>((options, configuration) =>
            {
                setupAction(options);
                configuration.GetSection("Geta:NotFoundHandler:Optimizely").Bind(options);
            });

            return services;
        }
    }
}
