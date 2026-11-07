// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Linq;
using Coravel;
using FakeItEasy;
using Geta.NotFoundHandler.Core.ScheduledJobs.Suggestions;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Geta.NotFoundHandler.Optimizely.Infrastructure.Initialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Geta.NotFoundHandler.Optimizely.Tests
{
    /// <summary>
    /// https://github.com/Geta/geta-notfoundhandler/issues/207
    /// </summary>
    public class InternalSchedulerTests
    {
        private readonly ILogger<SuggestionsCleanupJob> _logger = A.Fake<ILogger<SuggestionsCleanupJob>>();

        [Fact]
        public void Warns_when_the_flag_is_on()
        {
            Run(useInternalScheduler: true, hasScheduler: true);

            AssertWarnings(1);
        }

        /// <summary>
        /// A site can use Coravel for jobs of its own, and removing a job that isn't there still reports success.
        /// </summary>
        [Fact]
        public void Does_not_warn_on_a_site_using_the_scheduler_for_itself()
        {
            Run(useInternalScheduler: false, hasScheduler: true);

            AssertWarnings(0);
        }

        [Fact]
        public void Does_not_warn_without_a_scheduler()
        {
            Run(useInternalScheduler: false, hasScheduler: false);

            AssertWarnings(0);
        }

        private void Run(bool useInternalScheduler, bool hasScheduler)
        {
            var services = new ServiceCollection();
            services.AddSingleton(_logger);
            services.Configure<NotFoundHandlerOptions>(o => o.UseInternalScheduler = useInternalScheduler);

            if (hasScheduler)
            {
                services.AddScheduler();
            }

            using var serviceProvider = services.BuildServiceProvider();

            ApplicationBuilderExtensions.RemoveCleanupFromInternalScheduler(serviceProvider);
        }

        private void AssertWarnings(int expected)
        {
            var warnings = Fake.GetCalls(_logger)
                .Count(call => call.Method.Name == nameof(ILogger.Log) && call.GetArgument<LogLevel>(0) == LogLevel.Warning);

            Assert.Equal(expected, warnings);
        }
    }
}
