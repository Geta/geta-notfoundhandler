using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Web;
using Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects;

namespace Geta.NotFoundHandler.Optimizely.Commerce.AutomaticRedirects
{
    public class CommerceContentLinkProvider : IContentLinkProvider
    {
        private readonly ISiteDefinitionRepository _siteDefinitionRepository;
        private readonly IContentLoader _contentLoader;

        public CommerceContentLinkProvider(ISiteDefinitionRepository siteDefinitionRepository, IContentLoader contentLoader)
        {
            _siteDefinitionRepository = siteDefinitionRepository;
            _contentLoader = contentLoader;
        }

        public IEnumerable<ContentReference> GetAllLinks()
        {
            var allSites = _siteDefinitionRepository.List();
            return allSites.SelectMany(site => _contentLoader.GetDescendents(site.StartPage));
        }
    }
}
