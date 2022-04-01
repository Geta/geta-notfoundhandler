// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using EPiServer.Core;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public interface IContentUrlProvider
    {
        IEnumerable<TypedUrl> GetUrls(IContent content);
        bool CanHandle(IContent content);
    }
}
