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
        private string _loadedChecksum;
        private long _lastLoaded;

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
                // Note: Read before loading, so a save during the load causes another load rather than being missed.
                var checksum = _redirectLoader.GetChecksum();

                // Note: After the read above, as without a database the cached regex redirects are
                // all there is, and every unmatched request would fail trying to load them again.
                _regexRedirectCache.Remove();

                if (checksum == null)
                {
                    return;
                }

                var redirectsInitializer = _redirectsInitializerFactory();

                // Note: A load since the last check counts too. It can hold a redirect that has since been
                // removed again, in which case the stored redirects are back at the checksum of that check.
                if (checksum == _loadedChecksum && redirectsInitializer.LastLoaded == _lastLoaded)
                {
                    return;
                }

                redirectsInitializer.Initialize();
                _loadedChecksum = checksum;
                _lastLoaded = redirectsInitializer.LastLoaded;
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
