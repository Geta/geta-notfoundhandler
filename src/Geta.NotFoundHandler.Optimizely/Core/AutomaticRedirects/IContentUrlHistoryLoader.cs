using System.Collections.Generic;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public interface IContentUrlHistoryLoader
    {
        bool IsRegistered(ContentUrlHistory entity);
        IEnumerable<(string contentKey, IReadOnlyCollection<ContentUrlHistory> histories)> GetAllMoved();
        IReadOnlyCollection<ContentUrlHistory> GetMoved(string contentKey);
    }
}
