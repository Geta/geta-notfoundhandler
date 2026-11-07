// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Threading;
using Geta.NotFoundHandler.Data;

namespace Geta.NotFoundHandler.Core.Redirects
{
    public class RedirectsInitializer
    {
        private readonly CustomRedirectHandler _redirectHandler;
        private readonly Func<IRedirectsService> _redirectsServiceFactory;
        private readonly IEnumerable<INotFoundHandler> _providers;
        private readonly IRedirectLoader _redirectLoader;
        private readonly object _loadLock = new();
        private long _lastRequested;
        private long _lastLoaded;
        private volatile string _loadedChecksum;

        public RedirectsInitializer(
            RedirectsEvents redirectsEvents,
            CustomRedirectHandler redirectHandler,
            Func<IRedirectsService> redirectsServiceFactory,
            IEnumerable<INotFoundHandler> providers,
            IRedirectLoader redirectLoader)
        {
            _redirectHandler = redirectHandler;
            _redirectsServiceFactory = redirectsServiceFactory;
            _providers = providers;
            _redirectLoader = redirectLoader;
            redirectsEvents.OnRedirectsUpdated += OnRedirectsUpdated;
        }

        /// <summary>
        /// The checksum of the stored redirects when they were last loaded, whatever prompted it.
        /// </summary>
        internal string LoadedChecksum => _loadedChecksum;

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

                // Note: Read before the redirects, so a save in between causes another load rather than being missed.
                var checksum = ReadChecksum();
                var redirects = GetCustomRedirects();
                _redirectHandler.Set(redirects);
                _lastLoaded = loading;
                _loadedChecksum = checksum;
            }
        }

        /// <summary>
        /// The checksum only tells whether an update was missed, so a problem reading it
        /// mustn't stop a site from starting. The next check reloads, as it has none to compare to.
        /// </summary>
        private string ReadChecksum()
        {
            try
            {
                return _redirectLoader.GetChecksum();
            }
            catch (Exception)
            {
                return null;
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
