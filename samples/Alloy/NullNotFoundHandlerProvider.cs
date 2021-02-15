using Geta.NotFoundHandler.Core;

namespace AlloyMvcTemplates
{
    public class NullNotFoundHandlerProvider : INotFoundHandler
    {
        public string RewriteUrl(string url)
        {
            return null;
        }
    }
}