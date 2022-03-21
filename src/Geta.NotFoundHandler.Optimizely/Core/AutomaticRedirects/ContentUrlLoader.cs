using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Web.Routing;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class ContentUrlLoader
    {
        private readonly IEnumerable<IContentUrlProvider> _contentUrlProviders;
        private readonly IContentVersionRepository _contentVersionRepository;
        private readonly IContentLoader _contentLoader;
        private readonly IUrlResolver _urlResolver;

        public ContentUrlLoader(
            IEnumerable<IContentUrlProvider> contentUrlProviders,
            IContentVersionRepository contentVersionRepository,
            IContentLoader contentLoader,
            IUrlResolver urlResolver)
        {
            _contentUrlProviders = contentUrlProviders;
            _contentVersionRepository = contentVersionRepository;
            _contentLoader = contentLoader;
            _urlResolver = urlResolver;
        }

        public virtual IEnumerable<TypedUrl> GetUrls(ContentReference contentLink)
        {
            var lastPublishedVersion = _contentVersionRepository.LoadPublished(contentLink);
            if (lastPublishedVersion == null) return Enumerable.Empty<TypedUrl>();

            var content = _contentLoader.Get<IContent>(lastPublishedVersion.ContentLink);
            
            var contentUrls = _contentUrlProviders
                .SelectMany(provider => provider.GetUrls(content))
                .ToList();

            var handled = _contentUrlProviders.Any(x => x.CanHandle(content));

            if (!handled)
            {
                contentUrls.Add(GetFallbackUrl(content));
            }

            contentUrls = FilterEmpty(contentUrls);

            NormalizeUrls(contentUrls);

            return contentUrls;
        }

        private static List<TypedUrl> FilterEmpty(List<TypedUrl> contentUrls)
        {
            return contentUrls.Where(typedUrl => !string.IsNullOrEmpty(typedUrl.Url)).ToList();
        }

        private static void NormalizeUrls(List<TypedUrl> contentUrls)
        {
            foreach (var typedUrl in contentUrls)
            {
                typedUrl.Url =
                    (Uri.IsWellFormedUriString(typedUrl.Url, UriKind.Absolute)
                        ? new Uri(typedUrl.Url).PathAndQuery
                        : typedUrl.Url).Replace("//", "/");
            }
        }

        private TypedUrl GetFallbackUrl(IContent content)
        {
            return new TypedUrl { Url = _urlResolver.GetUrl(content.ContentLink), Type = UrlType.Primary };
        }
    }
}
