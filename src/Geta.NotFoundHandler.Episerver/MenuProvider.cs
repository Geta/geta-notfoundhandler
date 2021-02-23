using System.Collections.Generic;
using EPiServer.Framework.Localization;
using EPiServer.Security;
using EPiServer.Shell;
using EPiServer.Shell.Navigation;

namespace Geta.NotFoundHandler.Episerver
{
    [MenuProvider]
    public class MenuProvider : IMenuProvider
    {
        private readonly LocalizationService _localizationService;
        private readonly IPrincipalAccessor _principalAccessor;

        public MenuProvider(
            LocalizationService localizationService,
            IPrincipalAccessor principalAccessor)
        {
            _localizationService = localizationService;
            _principalAccessor = principalAccessor;
        }

        public IEnumerable<MenuItem> GetMenuItems()
        {
            var url = Paths.ToResource(GetType(), "container");

            var link = new UrlMenuItem(
                "NotFound handler",
                MenuPaths.Global + "/cms/notfoundhandler",
                url)
            {
                SortIndex = 100,
                AuthorizationPolicy = Constants.PolicyName 
            };

            return new List<MenuItem>
            {
                link
            };
        }
    }
}
