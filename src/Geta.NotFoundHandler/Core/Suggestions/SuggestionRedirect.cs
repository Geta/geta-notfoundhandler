namespace Geta.NotFoundHandler.Core.Suggestions
{
    public class SuggestionRedirect
    {
        public string OldUrl { get; }
        public string NewUrl { get; }

        public SuggestionRedirect(string oldUrl, string newUrl)
        {
            OldUrl = oldUrl;
            NewUrl = newUrl;
        }
    }
}
