// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
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
        private readonly object _initializeLock = new();
        private bool _initialized;

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

        /// <summary>
        /// Starts the sync between instances, unless it has already started.
        /// </summary>
        /// <returns>Whether this call started it.</returns>
        public bool Initialize()
        {
            // Note: Subscribing twice would reload the redirects twice for every update.
            lock (_initializeLock)
            {
                if (_initialized)
                {
                    return false;
                }

                // Note: Before subscribing to anything, so a failure here leaves nothing to undo on another try.
                var updateEvent = _eventRegistry.Get(UpdateEventId);
                var regexUpdateEvent = _eventRegistry.Get(RegexUpdateEventId);

                _redirectsEvents.OnRedirectsUpdated += OnRedirectsUpdated;
                _redirectsEvents.OnRegexRedirectsUpdated += OnRegexRedirectsUpdated;
                updateEvent.Raised += SyncRedirectsUpdateEventRaised;
                regexUpdateEvent.Raised += SyncRegexRedirectsUpdateEventRaised;

                _initialized = true;
                return true;
            }
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
