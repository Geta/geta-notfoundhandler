// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer.Core;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;
using Mediachase.Commerce.Catalog;

namespace Geta.NotFoundHandler.Optimizely.Commerce.AutomaticRedirects
{
    public class CommerceContentKeyProvider : IContentKeyProvider
    {
        private readonly ReferenceConverter _referenceConverter;

        public CommerceContentKeyProvider(ReferenceConverter referenceConverter)
        {
            _referenceConverter = referenceConverter;
        }

        public ContentKeyResult GetContentKey(ContentReference contentLink)
        {
            if (ContentReference.IsNullOrEmpty(contentLink) || contentLink.ProviderName != "CatalogContent")
            {
                return ContentKeyResult.Empty;
            }

            var contentType = _referenceConverter.GetContentType(contentLink);
            if (contentType == CatalogContentType.Root || contentType == CatalogContentType.Catalog)
            {
                return ContentKeyResult.Empty;
            }

            var code = _referenceConverter.GetCode(contentLink);
            if (string.IsNullOrWhiteSpace(code))
            {
                return ContentKeyResult.Empty;
            }

            return new ContentKeyResult($"{code}--{contentLink.ProviderName}");
        }
    }
}
