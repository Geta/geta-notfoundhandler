// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;

namespace Geta.NotFoundHandler.Core.Redirects
{
    public class RedirectsInitializer
    {
        private readonly CustomRedirectHandler _redirectHandler;
        private readonly Func<IRedirectsService> _redirectsServiceFactory;
        private readonly IEnumerable<INotFoundHandler> _providers;

        public RedirectsInitializer(
            RedirectsEvents redirectsEvents,
            CustomRedirectHandler redirectHandler,
            Func<IRedirectsService> redirectsServiceFactory,
            IEnumerable<INotFoundHandler> providers)
        {
            _redirectHandler = redirectHandler;
            _redirectsServiceFactory = redirectsServiceFactory;
            _providers = providers;
            redirectsEvents.OnUpdated += OnRedirectsUpdated;
        }

        private void OnRedirectsUpdated(EventArgs e)
        {
            Initialize();
        }

        public void Initialize()
        {
            var redirects = GetCustomRedirects();
            _redirectHandler.Set(redirects);
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
