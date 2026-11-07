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
        private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(1);
        private static readonly TimeSpan MaxInterval = TimeSpan.FromDays(30);

        private readonly Func<RedirectsInitializer> _redirectsInitializerFactory;
        private readonly IRedirectLoader _redirectLoader;
        private readonly IRegexRedirectCache _regexRedirectCache;
        private readonly ILogger<RedirectsRefreshService> _logger;
        private readonly TimeSpan _interval;

        public RedirectsRefreshService(
            Func<RedirectsInitializer> redirectsInitializerFactory,
            IRedirectLoader redirectLoader,
            IRegexRedirectCache regexRedirectCache,
            IOptions<NotFoundHandlerOptions> options,
            ILogger<RedirectsRefreshService> logger)
        {
            _redirectsInitializerFactory = redirectsInitializerFactory;
            _redirectLoader = redirectLoader;
            _regexRedirectCache = regexRedirectCache;
            _logger = logger;
            _interval = options.Value.RedirectsRefreshInterval;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Note: PeriodicTimer also throws below a millisecond, which is as good as turned off.
            if (_interval < MinInterval)
            {
                return;
            }

            // Note: PeriodicTimer throws above about 49 days, and an exception here would stop the application.
            using var timer = new PeriodicTimer(_interval < MaxInterval ? _interval : MaxInterval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                Refresh();
            }
        }

        /// <summary>
        /// Reloads the redirects if the stored ones differ from what this instance last loaded.
        /// </summary>
        public void Refresh()
        {
            try
            {
                var checksum = _redirectLoader.GetChecksum();
                if (checksum == null)
                {
                    return;
                }

                // Note: After the read above, as without a database the cached regex redirects are
                // all there is, and every unmatched request would fail trying to load them again.
                _regexRedirectCache.Remove();

                var redirectsInitializer = _redirectsInitializerFactory();
                if (checksum == redirectsInitializer.LoadedChecksum)
                {
                    return;
                }

                redirectsInitializer.Initialize();
                _logger.LogInformation(
                    "Reloaded the redirects, as the stored ones differ from the ones this instance had loaded. "
                    + "Either an update didn't reach this instance, or a save was in progress.");
            }
            catch (Exception ex)
            {
                // Note: The next check tries again, as a failed load doesn't change the loaded checksum.
                _logger.LogWarning(ex, "Failed to check for changed redirects. Will try again in {Interval}.", _interval);
            }
        }
    }
}
