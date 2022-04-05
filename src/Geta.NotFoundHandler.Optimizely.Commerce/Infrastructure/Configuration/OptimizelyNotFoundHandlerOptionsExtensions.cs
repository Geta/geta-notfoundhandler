// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Geta.NotFoundHandler.Optimizely.Commerce.AutomaticRedirects;
using Geta.NotFoundHandler.Optimizely.Infrastructure.Configuration;

namespace Geta.NotFoundHandler.Optimizely.Commerce.Infrastructure.Configuration
{
    public static class OptimizelyNotFoundHandlerOptionsExtensions
    {
        public static OptimizelyNotFoundHandlerOptions AddOptimizelyCommerceProviders(
            this OptimizelyNotFoundHandlerOptions options)
        {
            options.AddContentKeyProviders<CommerceContentKeyProvider>();
            options.AddContentLinkProviders<CommerceContentLinkProvider>();
            options.AddContentUrlProviders<EntryContentUrlProvider>();
            options.AddContentUrlProviders<NodeContentUrlProvider>();

            return options;
        }
    }
}
