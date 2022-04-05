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
    public class NodeContentUrlProvider : IContentUrlProvider
    {
        private readonly IUrlResolver _urlResolver;
        private readonly IRelationRepository _relationRepository;

        public NodeContentUrlProvider(IUrlResolver urlResolver, IRelationRepository relationRepository)
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

            var node = (NodeContent)content;

            return GetNodeContentUrls(node);
        }

        public bool CanHandle(IContent content)
        {
            return content is NodeContent;
        }

        private IEnumerable<TypedUrl> GetNodeContentUrls(NodeContent node)
        {
            var parentsLinks = _relationRepository
                .GetParents<NodeRelation>(node.ContentLink)
                .Select(x => x.Parent)
                .ToList();

            // primary parent node is not returned from _relationRepository.GetParents for nodes while it is for entries
            parentsLinks.Insert(0, node.ParentLink);
            parentsLinks = parentsLinks.Distinct().ToList();

            foreach (var parentLink in parentsLinks)
            {
                yield return new TypedUrl
                {
                    Url = $"{_urlResolver.GetUrl(parentLink)}/{node.RouteSegment}",
                    Type = parentLink == node.ParentLink ? UrlType.Primary : UrlType.Secondary
                };
            }

            yield return new TypedUrl { Url = $"/{node.SeoUri}", Type = UrlType.Seo };
        }
    }
}
