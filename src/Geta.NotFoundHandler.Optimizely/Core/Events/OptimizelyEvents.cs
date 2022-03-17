// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using EPiServer.Events.Clients;
using Geta.NotFoundHandler.Core.Redirects;

namespace Geta.NotFoundHandler.Optimizely.Core.Events
{
    public class OptimizelyEvents
    {
        private readonly RedirectsEvents _redirectsEvents;
        private readonly IEventRegistry _eventRegistry;
        private readonly RedirectsInitializer _redirectsInitializer;

        private static readonly Guid EventId = new("{AC263F88-6C17-45A5-81E0-DCC28DF26AEF}");
        private static readonly Guid RaiserId = Guid.NewGuid();

        public OptimizelyEvents(
            RedirectsEvents redirectsEvents,
            IEventRegistry eventRegistry,
            RedirectsInitializer redirectsInitializer)
        {
            _redirectsEvents = redirectsEvents;
            _eventRegistry = eventRegistry;
            _redirectsInitializer = redirectsInitializer;
        }

        public void Initialize()
        {
            _redirectsEvents.OnUpdated += OnRedirectsUpdated;
            _eventRegistry.Get(EventId).Raised += SyncEventRaised;
        }

        private void SyncEventRaised(object sender, EPiServer.Events.EventNotificationEventArgs e)
        {
            if (e.RaiserId != RaiserId)
            {
                _redirectsInitializer.Initialize();
            }
        }

        private void OnRedirectsUpdated(EventArgs e)
        {
            _eventRegistry.Get(EventId).Raise(RaiserId, EventId);
        }
    }
}
