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
