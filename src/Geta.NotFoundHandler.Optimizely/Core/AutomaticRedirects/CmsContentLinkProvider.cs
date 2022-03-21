using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Web;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class CmsContentLinkProvider : IContentLinkProvider
    {
        private readonly ISiteDefinitionRepository _siteDefinitionRepository;
        private readonly IContentRepository _contentRepository;

        public CmsContentLinkProvider(ISiteDefinitionRepository siteDefinitionRepository, IContentRepository contentRepository)
        {
            _siteDefinitionRepository = siteDefinitionRepository;
            _contentRepository = contentRepository;
        }

        public IEnumerable<ContentReference> GetAllLinks()
        {
            var allSites = _siteDefinitionRepository.List();
            return allSites.SelectMany(site => _contentRepository.GetDescendents(site.StartPage));
        }
    }
}
