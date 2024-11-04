// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Coravel;
using Geta.NotFoundHandler.Core.ScheduledJobs.Suggestions;
using Microsoft.AspNetCore.Builder;

namespace Geta.NotFoundHandler.Core.ScheduledJobs;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseScheduler(this IApplicationBuilder app)
    {
        var services = app.ApplicationServices;

        services.UseScheduler(scheduler =>
        {
            scheduler
                .Schedule<SuggestionsCleanupJob>()
                .Weekly()
                .PreventOverlapping(nameof(SuggestionsCleanupJob));
        });

        return app;
    }
}
