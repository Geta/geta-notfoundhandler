// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using Coravel.Scheduling.Schedule;
using Coravel.Scheduling.Schedule.Interfaces;
using Geta.NotFoundHandler.Core.ScheduledJobs.Suggestions;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Geta.NotFoundHandler.Optimizely.Core.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Optimizely.Infrastructure.Initialization
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseOptimizelyNotFoundHandler(this IApplicationBuilder app)
        {
            var services = app.ApplicationServices;

            var upgrader = services.GetRequiredService<Upgrader>();
            upgrader.Start();

            var syncEvents = services.GetRequiredService<OptimizelySyncEvents>();
            syncEvents.Initialize();

            var historyEvents = services.GetRequiredService<ContentUrlHistoryEvents>();
            historyEvents.Initialize();

            RemoveCleanupFromInternalScheduler(services);

            return app;
        }

        /// <summary>
        /// For optimizely we will use built-in scheduler for this job.
        /// </summary>
        internal static void RemoveCleanupFromInternalScheduler(IServiceProvider services)
        {
            var scheduler = services.GetService<IScheduler>();
            (scheduler as Scheduler)?.TryUnschedule(nameof(SuggestionsCleanupJob));

            // Note: Not from the result above, as that's also true when there was nothing to remove,
            // such as on a site using the scheduler for jobs of its own.
            if (services.GetRequiredService<IOptions<NotFoundHandlerOptions>>().Value.UseInternalScheduler)
            {
                services.GetRequiredService<ILogger<SuggestionsCleanupJob>>()
                    .LogWarning("UseInternalScheduler has no effect on an Optimizely site, so it can be removed. Suggestions are cleaned up by the [Geta NotFoundHandler] Suggestions cleanup job in the CMS, so make sure that's scheduled.");
            }
        }
    }
}
