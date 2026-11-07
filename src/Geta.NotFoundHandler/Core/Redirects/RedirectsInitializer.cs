// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Threading;

namespace Geta.NotFoundHandler.Core.Redirects
{
    public class RedirectsInitializer
    {
        private readonly CustomRedirectHandler _redirectHandler;
        private readonly Func<IRedirectsService> _redirectsServiceFactory;
        private readonly IEnumerable<INotFoundHandler> _providers;
        private readonly object _loadLock = new();
        private long _lastRequested;
        private long _lastLoaded;

        public RedirectsInitializer(
            RedirectsEvents redirectsEvents,
            CustomRedirectHandler redirectHandler,
            Func<IRedirectsService> redirectsServiceFactory,
            IEnumerable<INotFoundHandler> providers)
        {
            _redirectHandler = redirectHandler;
            _redirectsServiceFactory = redirectsServiceFactory;
            _providers = providers;
            redirectsEvents.OnRedirectsUpdated += OnRedirectsUpdated;
        }

        private void OnRedirectsUpdated(EventArgs e)
        {
            Initialize();
        }

        /// <summary>
        /// Loads the redirects. Only one load runs at a time, so a slow one that read the table before
        /// a save can't finish last and drop that redirect from this instance.
        /// </summary>
        public void Initialize()
        {
            var requested = Interlocked.Increment(ref _lastRequested);

            lock (_loadLock)
            {
                // Note: A load that started after this call covers whatever prompted it.
                if (_lastLoaded >= requested)
                {
                    return;
                }

                var loading = Volatile.Read(ref _lastRequested);
                var redirects = GetCustomRedirects();
                _redirectHandler.Set(redirects);
                _lastLoaded = loading;
            }
        }

        protected CustomRedirectCollection GetCustomRedirects()
        {
            var customRedirects = new CustomRedirectCollection(_providers);
            var redirectsService = _redirectsServiceFactory();

            foreach (var redirect in redirectsService.GetAll())
            {
                customRedirects.Add(redirect);
            }

            return customRedirects;
        }
    }
}
