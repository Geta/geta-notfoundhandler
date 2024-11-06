// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Core.ScheduledJobs;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Infrastructure.Initialization
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseNotFoundHandler(this IApplicationBuilder app)
        {
            var services = app.ApplicationServices;

            var upgrader = services.GetRequiredService<Upgrader>();
            upgrader.Start();

            var initializer = services.GetRequiredService<RedirectsInitializer>();
            initializer.Initialize();

            app.UseMiddleware<NotFoundHandlerMiddleware>();

            var options = services.GetRequiredService<IOptions<NotFoundHandlerOptions>>().Value;

            if (options.UseInternalScheduler)
            {
                app.UseScheduler();
            }

            return app;
        }
    }
}
