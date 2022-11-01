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
                .DistinctBy(x => new
                {
                    x.OldUrl,
                    x.NewUrl,
                    x.WildCardSkipAppend,
                    x.RedirectType
                })
                .Where(x => !string.IsNullOrEmpty(x.NewUrl));
        }

        private IEnumerable<CustomRedirect> CreateRedirects(ContentUrlHistory sourceHistory, ContentUrlHistory destinationHistory)
        {
            var languages = destinationHistory.Urls
                .Select(x => x.Language)
                .Distinct();

            foreach (var language in languages)
            {
                var languageSpecificDestinationUrls = destinationHistory.Urls
                    .Where(x => x.Language == language)
                    .ToList();
            
                var destinationPrimary = languageSpecificDestinationUrls.FirstOrDefault(x => x.Type == UrlType.Primary);
                if (destinationPrimary == null) yield break;

                foreach (var source in sourceHistory.Urls.Where(x => x.Language == language))
                {
                    var sourceUrl = source.Url;
                    var destinationUrl = GetDestinationUrl(source, languageSpecificDestinationUrls, destinationPrimary);

                    yield return new CustomRedirect(sourceUrl, destinationUrl, false, _configuration.AutomaticRedirectType);
                }
            }
        }

        private static string GetDestinationUrl(TypedUrl source, List<TypedUrl> destinationHistory, TypedUrl destinationPrimary)
        {
            switch (source.Type)
            {
                case UrlType.Primary:
                    return destinationPrimary.Url;
                case UrlType.Secondary:
                    var hasChanged = !destinationHistory.Any(x => x.Type == source.Type && x.Url == source.Url);
                    if (hasChanged)
                    {
                        // Always redirect to Primary as we do not know what and if there is a destination Secondary
                        return destinationPrimary.Url;
                    }

                    break;
                case UrlType.Seo:
                    var destinationSeo = destinationHistory.FirstOrDefault(x => x.Type == UrlType.Seo);
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
