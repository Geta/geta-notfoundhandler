// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Geta.NotFoundHandler.Core.ScheduledJobs;
using Geta.NotFoundHandler.Core.ScheduledJobs.Suggestions;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Geta.NotFoundHandler.Tests
{
    public class InternalSchedulerTests
    {
        [Fact]
        public void UseInternalScheduler_starts_without_a_non_generic_logger_registered()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddLogging();
            services.AddNotFoundHandler(o => o.UseInternalScheduler = true);
            using var serviceProvider = services.BuildServiceProvider();

            new ApplicationBuilder(serviceProvider).UseInternalScheduler();

            Assert.NotNull(serviceProvider.GetService<SuggestionsCleanupJob>());
        }
    }
}
