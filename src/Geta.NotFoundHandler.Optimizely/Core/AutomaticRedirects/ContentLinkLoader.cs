// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using EPiServer.Core;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class ContentLinkLoader
    {
        private readonly IEnumerable<IContentLinkProvider> _contentLinkProviders;

        public ContentLinkLoader(IEnumerable<IContentLinkProvider> contentLinkProviders)
        {
            _contentLinkProviders = contentLinkProviders;
        }

        public virtual IEnumerable<ContentReference> GetAllLinks()
        {
            return _contentLinkProviders.SelectMany(provider => provider.GetAllLinks()).Distinct().ToList();
        }
    }
}
