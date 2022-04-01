using EPiServer.Core;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public interface IMovedContentRegistratorQueue
    {
        void Enqueue(ContentReference contentLink);
    }
}
