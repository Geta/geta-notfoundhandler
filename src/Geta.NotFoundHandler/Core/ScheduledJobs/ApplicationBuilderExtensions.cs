// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Coravel;
using Geta.NotFoundHandler.Core.ScheduledJobs.Suggestions;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Core.ScheduledJobs;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseScheduler(this IApplicationBuilder app)
    {
        var services = app.ApplicationServices;

        var options = services.GetRequiredService<IOptions<NotFoundHandlerOptions>>().Value;
        
        services.UseScheduler(scheduler =>
        {
            scheduler
                .Schedule<SuggestionsCleanupJob>()
                .Cron(options.SuggestionsCleanupOptions.CronInterval)
                .PreventOverlapping(nameof(SuggestionsCleanupJob));
        });

        return app;
    }
}
