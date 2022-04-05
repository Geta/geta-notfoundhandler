// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer.Core;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class CmsContentKeyProvider : IContentKeyProvider
    {
        public ContentKeyResult GetContentKey(ContentReference contentLink)
        {
            if (ContentReference.IsNullOrEmpty(contentLink) || contentLink.ProviderName != null || contentLink.IsExternalProvider)
            {
                return ContentKeyResult.Empty;
            }

            var key = contentLink.ToReferenceWithoutVersion().ToString();
            return new ContentKeyResult(key);
        }
    }
}
