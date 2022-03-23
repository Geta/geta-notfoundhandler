namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public interface IContentUrlHistoryLoader
    {
        bool IsRegistered(ContentUrlHistory entity);
    }
}
