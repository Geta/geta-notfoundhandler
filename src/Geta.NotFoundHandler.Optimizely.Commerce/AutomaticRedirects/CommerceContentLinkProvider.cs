// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Mediachase.Commerce.Catalog;

namespace Geta.NotFoundHandler.Optimizely.Commerce.AutomaticRedirects
{
    public class CommerceContentLinkProvider : IContentLinkProvider
    {
        private readonly ReferenceConverter _referenceConverter;
        private readonly IContentLoader _contentLoader;

        public CommerceContentLinkProvider(ReferenceConverter referenceConverter, IContentLoader contentLoader)
        {
            _referenceConverter = referenceConverter;
            _contentLoader = contentLoader;
        }

        public IEnumerable<ContentReference> GetAllLinks()
        {
            return _contentLoader.GetDescendents(_referenceConverter.GetRootLink()).ToList();
        }
    }
}
