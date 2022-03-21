using EPiServer.Core;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public interface IContentKeyProvider
    {
        ContentKeyResult GetContentKey(ContentReference contentLink);
    }
}
