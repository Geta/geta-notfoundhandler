using System.Collections.Generic;
using System.Linq;
using Geta.NotFoundHandler.Core.Redirects;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class DefaultAutomaticRedirectsService : IAutomaticRedirectsService
    {
        private readonly IRedirectsService _redirectsService;
        private readonly RedirectBuilder _redirectBuilder;

        public DefaultAutomaticRedirectsService(
            IRedirectsService redirectsService,
            RedirectBuilder redirectBuilder)
        {
            _redirectsService = redirectsService;
            _redirectBuilder = redirectBuilder;
        }

        public void CreateRedirects(IReadOnlyCollection<ContentUrlHistory> histories)
        {
            var redirects = _redirectBuilder.CreateRedirects(histories).ToList();
            _redirectsService.AddOrUpdate(redirects);
            var urlsToRemove = redirects.Where(x => x.NewUrl == x.OldUrl).Select(x => x.OldUrl);
            _redirectsService.DeleteByOldUrl(urlsToRemove);
        }
    }
}
