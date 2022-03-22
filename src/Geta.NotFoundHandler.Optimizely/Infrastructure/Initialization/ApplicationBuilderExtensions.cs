// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Geta.NotFoundHandler.Optimizely.Core.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Geta.NotFoundHandler.Optimizely.Infrastructure.Initialization
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseOptimizelyNotFoundHandler(this IApplicationBuilder app)
        {
            var services = app.ApplicationServices;

            var upgrader = services.GetRequiredService<Upgrader>();
            upgrader.Start();

            var initializer = services.GetRequiredService<OptimizelySyncEvents>();
            initializer.Initialize();

            return app;
        }
    }
}
