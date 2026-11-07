// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Geta.NotFoundHandler.Optimizely.Core.Events
{
    /// <summary>
    /// Leaving out UseOptimizelyNotFoundHandler works fine on a single server, and only shows on several
    /// servers as redirects not reaching all of them. This makes the sync work anyway, and says so in the log.
    /// </summary>
    public class OptimizelySyncEventsStartupCheck : IHostedService
    {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IServiceProvider _services;
        private readonly ILogger<OptimizelySyncEventsStartupCheck> _logger;

        public OptimizelySyncEventsStartupCheck(
            IHostApplicationLifetime lifetime,
            IServiceProvider services,
            ILogger<OptimizelySyncEventsStartupCheck> logger)
        {
            _lifetime = lifetime;
            _services = services;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Note: After the application has started, as that's when UseOptimizelyNotFoundHandler has had its chance.
            _lifetime.ApplicationStarted.Register(EnsureInitialized);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        internal void EnsureInitialized()
        {
            var syncEvents = _services.GetRequiredService<OptimizelySyncEvents>();
            if (syncEvents.IsInitialized)
            {
                return;
            }

            _logger.LogWarning(
                "UseOptimizelyNotFoundHandler has not been called, so NotFoundHandler has started the sync between "
                + "servers on its own. Add it next to UseNotFoundHandler, as automatic redirects and database "
                + "upgrades for them also depend on it.");

            syncEvents.Initialize();
        }
    }
}
