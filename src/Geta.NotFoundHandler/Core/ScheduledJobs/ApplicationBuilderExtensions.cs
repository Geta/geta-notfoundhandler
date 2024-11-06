// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Coravel;
using Geta.NotFoundHandler.Core.ScheduledJobs.Suggestions;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Core.ScheduledJobs;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseScheduler(this IApplicationBuilder app)
    {
        var services = app.ApplicationServices;

        var options = services.GetRequiredService<IOptions<NotFoundHandlerOptions>>().Value;
        var logger = services.GetRequiredService<ILogger>();

        services.UseScheduler(scheduler =>
            {
                scheduler
                    .Schedule<SuggestionsCleanupJob>()
                    .Cron(options.InternalSchedulerCronInterval)
                    .PreventOverlapping(nameof(SuggestionsCleanupJob));
            })
            .OnError(x =>
            {
                logger.LogError(x, "Something went wrong, scheduled job fails with exception");
            });

        return app;
    }
}
