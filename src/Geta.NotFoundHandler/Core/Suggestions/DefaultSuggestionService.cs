using System.Collections.Generic;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Data;

namespace Geta.NotFoundHandler.Core.Suggestions
{
    public class DefaultSuggestionService : ISuggestionService
    {
        private readonly ISuggestionLoader _suggestionLoader;
        private readonly IRedirectsService _redirectsService;

        public DefaultSuggestionService(
            ISuggestionLoader suggestionLoader,
            IRedirectsService redirectsService)
        {
            _suggestionLoader = suggestionLoader;
            _redirectsService = redirectsService;
        }

        public IEnumerable<SuggestionSummary> GetAllSummaries()
        {
            return _suggestionLoader.GetAllSummaries();
        }

        public void AddRedirect(SuggestionRedirect suggestionRedirect)
        {
            SaveRedirect(suggestionRedirect);
            DeleteSuggestionsFor(suggestionRedirect.OldUrl);
        }

        public void IgnoreSuggestion(string oldUrl)
        {
            SaveIgnoredRedirect(oldUrl);
            DeleteSuggestionsFor(oldUrl);
        }

        public void DeleteAll()
        {
            var worker = DataAccessBaseEx.GetWorker();
            worker.DeleteAllSuggestions();
        }

        public void Delete(int maxErrors, int minimumDays)
        {
            var worker = DataAccessBaseEx.GetWorker();
            worker.DeleteSuggestions(maxErrors, minimumDays);
        }

        private void SaveIgnoredRedirect(string oldUrl)
        {
            var customRedirect = new CustomRedirect(oldUrl, RedirectState.Ignored);
            _redirectsService.AddOrUpdate(customRedirect);
        }

        private void SaveRedirect(SuggestionRedirect suggestionRedirect)
        {
            var customRedirect = new CustomRedirect(suggestionRedirect.OldUrl, suggestionRedirect.NewUrl);
            _redirectsService.AddOrUpdate(customRedirect);
        }

        private void DeleteSuggestionsFor(string oldUrl)
        {
            var worker = DataAccessBaseEx.GetWorker();
            worker.DeleteSuggestionsForRequest(oldUrl);
        }
    }
}
