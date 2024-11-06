// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;

namespace Geta.NotFoundHandler.Optimizely.Infrastructure.Configuration
{
    public class OptimizelyNotFoundHandlerOptions
    {
        public const int CurrentDbVersion = 3;

        public bool AutomaticRedirectsEnabled { get; set; }
        public RedirectType AutomaticRedirectType { get; set; } = RedirectType.Temporary;

        private readonly List<Type> _contentKeyProviders = new();
        public IEnumerable<Type> ContentKeyProviders => _contentKeyProviders;

        private readonly List<Type> _contentLinkProviders = new();
        public IEnumerable<Type> ContentLinkProviders => _contentLinkProviders;

        private readonly List<Type> _contentUrlProviders = new();
        public IEnumerable<Type> ContentUrlProviders => _contentUrlProviders;

        public OptimizelyNotFoundHandlerOptions()
        {
            AddContentKeyProviders<CmsContentKeyProvider>();
            AddContentLinkProviders<CmsContentLinkProvider>();
            AddContentUrlProviders<CmsContentUrlProvider>();
        }

        public OptimizelyNotFoundHandlerOptions AddContentKeyProviders<T>()
            where T : IContentKeyProvider
        {
            var t = typeof(T);
            _contentKeyProviders.Add(t);
            return this;
        }

        public OptimizelyNotFoundHandlerOptions AddContentLinkProviders<T>()
            where T : IContentLinkProvider
        {
            var t = typeof(T);
            _contentLinkProviders.Add(t);
            return this;
        }

        public OptimizelyNotFoundHandlerOptions AddContentUrlProviders<T>()
            where T : IContentUrlProvider
        {
            var t = typeof(T);
            _contentUrlProviders.Add(t);
            return this;
        }
    }
}
