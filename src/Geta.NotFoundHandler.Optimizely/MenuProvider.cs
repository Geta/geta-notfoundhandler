using System.Collections.Generic;
using EPiServer.Shell;
using EPiServer.Shell.Navigation;

namespace Geta.NotFoundHandler.Optimizely
{
    [MenuProvider]
    public class MenuProvider : IMenuProvider
    {
        public IEnumerable<MenuItem> GetMenuItems()
        {
            var url = Paths.ToResource(GetType(), "container");

            var link = new UrlMenuItem(
                "NotFound handler",
                MenuPaths.Global + "/cms/notfoundhandler",
                url)
            {
                SortIndex = 100,
                AuthorizationPolicy = Infrastructure.Constants.PolicyName
            };

            return new List<MenuItem>
            {
                link
            };
        }
    }
}
