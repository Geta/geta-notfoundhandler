// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using EPiServer.Core;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class ContentKeyGenerator
    {
        private readonly IEnumerable<IContentKeyProvider> _contentKeyProviders;

        public ContentKeyGenerator(IEnumerable<IContentKeyProvider> contentKeyProviders)
        {
            _contentKeyProviders = contentKeyProviders;
        }

        public virtual ContentKeyResult GetContentKey(ContentReference contentLink)
        {
            if (ContentReference.IsNullOrEmpty(contentLink))
            {
                return ContentKeyResult.Empty;
            }

            return _contentKeyProviders
                       .Select(provider => provider.GetContentKey(contentLink))
                       .FirstOrDefault(result => result.HasValue)
                   ?? ContentKeyResult.Empty;
        }
    }
}
