// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Coravel;
using Geta.NotFoundHandler.Core.ScheduledJobs.Suggestions;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Core.ScheduledJobs;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection EnableScheduler(this IServiceCollection services)
    {
        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<NotFoundHandlerOptions>>().Value;

        if (options.UseScheduler)
        {
            services.AddScheduler();
        
            services.AddTransient<SuggestionsCleanupJob>();
        }

        return services;
    }
}
