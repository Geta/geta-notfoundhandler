// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Threading;
using System.Threading.Tasks;
using Geta.NotFoundHandler.Core.Providers.RegexRedirects;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Core.Redirects
{
    /// <summary>
    /// Keeps the redirects of this instance in line with the database when an update never reached it,
    /// such as a lost event, a reload that failed, or an instance starting while redirects were saved.
    /// </summary>
    public class RedirectsRefreshService : BackgroundService
    {
        private static readonly TimeSpan MaxInterval = TimeSpan.FromDays(30);

        private readonly RedirectsInitializer _redirectsInitializer;
        private readonly IRedirectLoader _redirectLoader;
        private readonly IRegexRedirectCache _regexRedirectCache;
        private readonly ILogger<RedirectsRefreshService> _logger;
        private readonly TimeSpan _interval;
        private string _loadedChecksum;

        public RedirectsRefreshService(
            RedirectsInitializer redirectsInitializer,
            IRedirectLoader redirectLoader,
            IRegexRedirectCache regexRedirectCache,
            IOptions<NotFoundHandlerOptions> options,
            ILogger<RedirectsRefreshService> logger)
        {
            _redirectsInitializer = redirectsInitializer;
            _redirectLoader = redirectLoader;
            _regexRedirectCache = regexRedirectCache;
            _logger = logger;
            _interval = options.Value.RedirectsRefreshInterval;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_interval <= TimeSpan.Zero)
            {
                return;
            }

            // Note: PeriodicTimer throws above about 49 days, and an exception here would stop the application.
            using var timer = new PeriodicTimer(_interval < MaxInterval ? _interval : MaxInterval);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    Refresh();
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // The application is stopping.
            }
        }

        /// <summary>
        /// Reloads the redirects if the stored ones changed since the last check.
        /// </summary>
        public void Refresh()
        {
            try
            {
                _regexRedirectCache.Remove();

                // Note: Read before loading, so a save during the load causes another load rather than being missed.
                var checksum = _redirectLoader.GetChecksum();
                if (checksum == null || checksum == _loadedChecksum)
                {
                    return;
                }

                _redirectsInitializer.Initialize();
                _loadedChecksum = checksum;
                _logger.LogDebug("Reloaded the redirects, as the stored ones have changed since the last check.");
            }
            catch (Exception ex)
            {
                // Note: Keeps the last checksum, so the next check tries again.
                _logger.LogWarning(ex, "Failed to check for changed redirects. Will try again in {Interval}.", _interval);
            }
        }
    }
}
