// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Optimizely.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class RedirectBuilder
    {
        private readonly OptimizelyNotFoundHandlerOptions _configuration;

        public RedirectBuilder(IOptions<OptimizelyNotFoundHandlerOptions> options)
        {
            _configuration = options.Value;
        }

        public virtual IEnumerable<CustomRedirect> CreateRedirects(IReadOnlyCollection<ContentUrlHistory> histories)
        {
            var ordered = histories.OrderByDescending(x => x.CreatedUtc).ToList();
            var destination = ordered.First();

            return ordered
                .Skip(1)
                .SelectMany(source => CreateRedirects(source, destination))
                .Where(x => !string.IsNullOrEmpty(x.NewUrl));
        }

        private IEnumerable<CustomRedirect> CreateRedirects(ContentUrlHistory sourceHistory, ContentUrlHistory destinationHistory)
        {
            var destinationPrimary = destinationHistory.Urls.FirstOrDefault(x => x.Type == UrlType.Primary);
            if (destinationPrimary == null) yield break;

            foreach (var source in sourceHistory.Urls)
            {
                var sourceUrl = source.Url;
                var destinationUrl = GetDestinationUrl(source, destinationHistory, destinationPrimary);

                yield return new CustomRedirect(sourceUrl, destinationUrl, false, _configuration.AutomaticRedirectType);
            }
        }

        private static string GetDestinationUrl(TypedUrl source, ContentUrlHistory destinationHistory, TypedUrl destinationPrimary)
        {
            switch (source.Type)
            {
                case UrlType.Primary:
                    return destinationPrimary.Url;
                case UrlType.Secondary:
                    var hasChanged = !destinationHistory.Urls.Any(x => x.Type == source.Type && x.Url == source.Url);
                    if (hasChanged)
                    {
                        // Always redirect to Primary as we do not know what and if there is a destination Secondary
                        return destinationPrimary.Url;
                    }

                    break;
                case UrlType.Seo:
                    var destinationSeo = destinationHistory.Urls.FirstOrDefault(x => x.Type == UrlType.Seo);
                    if (destinationSeo != null)
                    {
                        return destinationSeo.Url;
                    }
                    else // Fallback to Primary
                    {
                        return destinationPrimary.Url;
                    }
            }

            return null;
        }
    }
}
