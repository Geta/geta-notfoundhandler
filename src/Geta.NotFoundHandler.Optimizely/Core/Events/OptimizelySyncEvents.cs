// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Threading;
using EPiServer.Events.Clients;
using Geta.NotFoundHandler.Core.Providers.RegexRedirects;
using Geta.NotFoundHandler.Core.Redirects;
using Microsoft.Extensions.Logging;

namespace Geta.NotFoundHandler.Optimizely.Core.Events
{
    public class OptimizelySyncEvents
    {
        private readonly RedirectsEvents _redirectsEvents;
        private readonly IEventRegistry _eventRegistry;
        private readonly RedirectsInitializer _redirectsInitializer;
        private readonly IRegexRedirectCache _regexRedirectCache;
        private readonly ILogger<OptimizelySyncEvents> _logger;
        private int _initialized;

        private static readonly Guid UpdateEventId = new("{AC263F88-6C17-45A5-81E0-DCC28DF26AEF}");
        private static readonly Guid RegexUpdateEventId = new("{334DF0A0-793C-4B19-A0CC-8AD63F705FDD}");
        private static readonly Guid RaiserId = Guid.NewGuid();

        public OptimizelySyncEvents(
            RedirectsEvents redirectsEvents,
            IEventRegistry eventRegistry,
            RedirectsInitializer redirectsInitializer,
            IRegexRedirectCache regexRedirectCache,
            ILogger<OptimizelySyncEvents> logger)
        {
            _redirectsEvents = redirectsEvents;
            _eventRegistry = eventRegistry;
            _redirectsInitializer = redirectsInitializer;
            _regexRedirectCache = regexRedirectCache;
            _logger = logger;
        }

        public bool IsInitialized => Volatile.Read(ref _initialized) == 1;

        public void Initialize()
        {
            // Note: Subscribing twice would reload the redirects twice for every update.
            if (Interlocked.Exchange(ref _initialized, 1) == 1)
            {
                return;
            }

            _redirectsEvents.OnRedirectsUpdated += OnRedirectsUpdated;
            _redirectsEvents.OnRegexRedirectsUpdated += OnRegexRedirectsUpdated;
            _eventRegistry.Get(UpdateEventId).Raised += SyncRedirectsUpdateEventRaised;
            _eventRegistry.Get(RegexUpdateEventId).Raised += SyncRegexRedirectsUpdateEventRaised;
        }

        private void SyncRedirectsUpdateEventRaised(object sender, EPiServer.Events.EventNotificationEventArgs e)
        {
            if (e.RaiserId == RaiserId)
            {
                return;
            }

            try
            {
                _redirectsInitializer.Initialize();
            }
            catch (Exception ex)
            {
                // Note: Nothing above this can handle it. The periodic check picks up the update later.
                _logger.LogError(ex, "Failed to reload the redirects after an update on another instance.");
            }
        }

        private void SyncRegexRedirectsUpdateEventRaised(object sender, EPiServer.Events.EventNotificationEventArgs e)
        {
            if (e.RaiserId != RaiserId)
            {
                _regexRedirectCache.Remove();
            }
        }

        private void OnRedirectsUpdated(EventArgs e)
        {
            _eventRegistry.Get(UpdateEventId).Raise(RaiserId, UpdateEventId);
        }

        private void OnRegexRedirectsUpdated(EventArgs e)
        {
            _eventRegistry.Get(RegexUpdateEventId).Raise(RaiserId, RegexUpdateEventId);
        }
    }
}
