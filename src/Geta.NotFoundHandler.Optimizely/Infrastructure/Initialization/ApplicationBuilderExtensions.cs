// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Coravel.Scheduling.Schedule;
using Coravel.Scheduling.Schedule.Interfaces;
using Geta.NotFoundHandler.Core.ScheduledJobs.Suggestions;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Geta.NotFoundHandler.Optimizely.Core.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

            // For optimizely we will use built-in scheduler for this job
            var scheduler = services.GetService<IScheduler>();
            if ((scheduler as Scheduler)?.TryUnschedule(nameof(SuggestionsCleanupJob)) == true)
            {
                services.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(ApplicationBuilderExtensions))
                    .LogWarning("UseInternalScheduler has no effect on the suggestions cleanup on Optimizely sites. Schedule the [Geta NotFoundHandler] Suggestions cleanup job in the CMS instead.");
            }

            return app;
        }
    }
}
