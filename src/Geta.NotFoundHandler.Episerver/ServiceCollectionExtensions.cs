using System;
using System.Linq;
using EPiServer.DependencyInjection;
using EPiServer.Shell.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Geta.NotFoundHandler.Episerver
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEpiserverNotFoundHandler(this IServiceCollection services)
        {
            services.AddCmsUI();
            services.Configure<ProtectedModuleOptions>(
                pm =>
                {
                    if (!pm.Items.Any(i => i.Name.Equals(Constants.ModuleName, StringComparison.OrdinalIgnoreCase)))
                    {
                        pm.Items.Add(new ModuleDetails { Name = Constants.ModuleName });
                    }
                });

            return services;
        }
    }
}
