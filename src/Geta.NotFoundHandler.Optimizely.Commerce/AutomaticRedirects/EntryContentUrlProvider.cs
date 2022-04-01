// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Core;
using EPiServer.Web.Routing;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;

namespace Geta.NotFoundHandler.Optimizely.Commerce.AutomaticRedirects
{
    public class EntryContentUrlProvider : IContentUrlProvider
    {
        private readonly IUrlResolver _urlResolver;
        private readonly IRelationRepository _relationRepository;

        public EntryContentUrlProvider(IUrlResolver urlResolver, IRelationRepository relationRepository)
        {
            _urlResolver = urlResolver;
            _relationRepository = relationRepository;
        }

        public IEnumerable<TypedUrl> GetUrls(IContent content)
        {
            if (!CanHandle(content))
            {
                return Enumerable.Empty<TypedUrl>();
            }

            var entry = (EntryContentBase)content;

            return GetNodeContentUrls(entry);
        }

        public bool CanHandle(IContent content)
        {
            return content is EntryContentBase;
        }

        private IEnumerable<TypedUrl> GetNodeContentUrls(EntryContentBase entry)
        {
            var parentsLinks = _relationRepository
                .GetParents<NodeEntryRelation>(entry.ContentLink)
                .ToList();

            foreach (var nodeParent in parentsLinks)
            {
                yield return new TypedUrl
                {
                    Url = $"{_urlResolver.GetUrl(nodeParent.Parent)}/{entry.RouteSegment}",
                    Type = nodeParent.IsPrimary ? UrlType.Primary : UrlType.Secondary
                };
            }

            yield return new TypedUrl { Url = $"/{entry.SeoUri}", Type = UrlType.Seo };
        }
    }
}
