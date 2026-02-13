// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using Coravel;
using Geta.NotFoundHandler.Core.ScheduledJobs.Suggestions;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Geta.NotFoundHandler.Core.ScheduledJobs;

public static class ServiceCollectionExtensions
{
    [Obsolete("Use EnableScheduler with IConfigurationSection parameter instead.")]
    public static IServiceCollection EnableScheduler(
        this IServiceCollection services)
    {
        return services.EnableScheduler(null);
    }

    public static IServiceCollection EnableScheduler(
        this IServiceCollection services,
        IConfigurationSection notFoundHandlerConfiguration)
    {
        var options = notFoundHandlerConfiguration?.Get<NotFoundHandlerOptions>();

        if (!(options?.UseInternalScheduler ?? false))
        {
            return services;
        }

        services.AddScheduler();
        services.AddTransient<SuggestionsCleanupJob>();

        return services;
    }
}
