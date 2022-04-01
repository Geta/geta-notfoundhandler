// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public record TypedUrl
    {
        public string Url { get; set; }
        public UrlType Type { get; set; }
    }
}
