using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Web.Routing;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class CmsContentUrlProvider : IContentUrlProvider
    {
        private readonly IContentRepository _contentRepository;
        private readonly IContentVersionRepository _contentVersionRepository;
        private readonly IUrlResolver _urlResolver;

        public CmsContentUrlProvider(
            IContentRepository contentRepository,
            IContentVersionRepository contentVersionRepository,
            IUrlResolver urlResolver)
        {
            _contentRepository = contentRepository;
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

        private IEnumerable<TypedUrl> GetPageUrls(PageData page)
        {
            return new List<TypedUrl> { new() { Url = GetPageUrl(page), Type = UrlType.Primary } };
        }

        private string GetPageUrl(PageData page)
        {
            if (page.LinkType == PageShortcutType.External || page.LinkType == PageShortcutType.Shortcut)
            {
                var lastPublishedVersion = _contentVersionRepository.LoadPublished(page.ParentLink);
                var parent = _contentRepository.Get<IContent>(lastPublishedVersion.ContentLink);
                if (parent is PageData parentPage)
                {
                    var parentUrl = GetPageUrl(parentPage);
                    return $"{parentUrl}/{page.URLSegment}/";
                }

                return "/";
            }

            return _urlResolver.GetUrl(page.ContentLink);
        }

        public bool CanHandle(IContent content)
        {
            return content is PageData;
        }
    }
}
