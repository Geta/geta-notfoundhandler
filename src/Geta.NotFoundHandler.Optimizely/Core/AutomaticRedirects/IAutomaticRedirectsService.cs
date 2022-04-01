using System.Collections.Generic;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public interface IAutomaticRedirectsService
    {
        void CreateRedirects(IReadOnlyCollection<ContentUrlHistory> histories);
    }
}
