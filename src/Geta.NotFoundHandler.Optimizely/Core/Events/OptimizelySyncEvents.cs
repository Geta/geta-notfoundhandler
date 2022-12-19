// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using EPiServer.Events.Clients;
using Geta.NotFoundHandler.Core.Providers.RegexRedirects;
using Geta.NotFoundHandler.Core.Redirects;

namespace Geta.NotFoundHandler.Optimizely.Core.Events
{
    public class OptimizelySyncEvents
    {
        private readonly RedirectsEvents _redirectsEvents;
        private readonly IEventRegistry _eventRegistry;
        private readonly RedirectsInitializer _redirectsInitializer;
        private readonly IRegexRedirectCache _regexRedirectCache;

        private static readonly Guid UpdateEventId = new("{AC263F88-6C17-45A5-81E0-DCC28DF26AEF}");
        private static readonly Guid RegexUpdateEventId = new("{334DF0A0-793C-4B19-A0CC-8AD63F705FDD}");
        private static readonly Guid RaiserId = Guid.NewGuid();

        public OptimizelySyncEvents(
            RedirectsEvents redirectsEvents,
            IEventRegistry eventRegistry,
            RedirectsInitializer redirectsInitializer,
            IRegexRedirectCache regexRedirectCache)
        {
            _redirectsEvents = redirectsEvents;
            _eventRegistry = eventRegistry;
            _redirectsInitializer = redirectsInitializer;
            _regexRedirectCache = regexRedirectCache;
        }

        public void Initialize()
        {
            _redirectsEvents.OnRedirectsUpdated += OnRedirectsUpdated;
            _redirectsEvents.OnRegexRedirectsUpdated += OnRegexRedirectsUpdated;
            _eventRegistry.Get(UpdateEventId).Raised += SyncUpdateEventRaised;
            _eventRegistry.Get(RegexUpdateEventId).Raised += SyncRegexUpdateEventRaised;
        }

        private void SyncUpdateEventRaised(object sender, EPiServer.Events.EventNotificationEventArgs e)
        {
            if (e.RaiserId != RaiserId)
            {
                _redirectsInitializer.Initialize();
            }
        }

        private void SyncRegexUpdateEventRaised(object sender, EPiServer.Events.EventNotificationEventArgs e)
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
