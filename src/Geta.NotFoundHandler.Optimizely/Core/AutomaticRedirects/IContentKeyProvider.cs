// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer.Core;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public interface IContentKeyProvider
    {
        ContentKeyResult GetContentKey(ContentReference contentLink);
    }
}
