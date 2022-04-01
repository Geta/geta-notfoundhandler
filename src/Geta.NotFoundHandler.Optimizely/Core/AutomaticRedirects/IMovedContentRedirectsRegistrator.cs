using EPiServer.Core;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public interface IMovedContentRedirectsRegistrator
    {
        void Register(ContentReference contentLink);
    }
}
