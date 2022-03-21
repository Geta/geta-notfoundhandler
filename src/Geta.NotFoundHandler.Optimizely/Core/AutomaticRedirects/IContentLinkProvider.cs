using System.Collections.Generic;
using EPiServer.Core;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public interface IContentLinkProvider
    {
        IEnumerable<ContentReference> GetAllLinks();
    }
}
