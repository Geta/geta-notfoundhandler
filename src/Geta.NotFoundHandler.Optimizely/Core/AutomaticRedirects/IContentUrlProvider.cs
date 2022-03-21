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
