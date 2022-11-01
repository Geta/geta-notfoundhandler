// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Cms.Shell;
using EPiServer.Core;
using EPiServer.Web.Routing;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class CmsContentUrlProvider : IContentUrlProvider
    {
        private readonly IContentLoader _contentLoader;
        private readonly IContentVersionRepository _contentVersionRepository;
        private readonly IUrlResolver _urlResolver;

        public CmsContentUrlProvider(
            IContentLoader contentLoader,
            IContentVersionRepository contentVersionRepository,
            IUrlResolver urlResolver)
        {
            _contentLoader = contentLoader;
            _contentVersionRepository = contentVersionRepository;
            _urlResolver = urlResolver;
        }

        public IEnumerable<TypedUrl> GetUrls(IContent content)
        {
            if (!CanHandle(content))
            {
                return Enumerable.Empty<TypedUrl>();
            }

            var page = (PageData)content;
            if (page.StopPublish <= DateTime.UtcNow)
            {
                return Enumerable.Empty<TypedUrl>();
            }

            return GetPageUrls(page);
        }

        public bool CanHandle(IContent content)
        {
            return content is PageData;
        }

        private IEnumerable<TypedUrl> GetPageUrls(PageData page)
        {
            return new List<TypedUrl> { new() { Url = GetPageUrl(page), Type = UrlType.Primary, Language = page.LanguageBranch()} };
        }

        private string GetPageUrl(PageData page)
        {
            if (page.LinkType == PageShortcutType.External || page.LinkType == PageShortcutType.Shortcut)
            {
                var lastPublishedVersion = _contentVersionRepository.LoadPublished(page.ParentLink);
                var parent = _contentLoader.Get<IContent>(lastPublishedVersion.ContentLink);
                if (parent is PageData parentPage)
                {
                    var parentUrl = GetPageUrl(parentPage);
                    return $"{parentUrl}/{page.URLSegment}/";
                }

                return "/";
            }

            return _urlResolver.GetUrl(page.ContentLink);
        }
    }
}
