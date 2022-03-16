// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Geta.NotFoundHandler.Optimizely
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseOptimizelyNotFoundHandler(this IApplicationBuilder app)
        {
            var services = app.ApplicationServices;

            var initializer = services.GetRequiredService<OptimizelyEvents>();
            initializer.Initialize();

            return app;
        }
    }
}
