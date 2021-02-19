using System.Collections.Generic;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Data;

namespace Geta.NotFoundHandler.Core.Suggestions
{
    public class DefaultSuggestionService : ISuggestionService
    {
        private readonly ISuggestionLoader _suggestionLoader;
        private readonly IRepository<Suggestion> _repository;
        private readonly IRedirectsService _redirectsService;

        public DefaultSuggestionService(
            ISuggestionLoader suggestionLoader,
            IRepository<Suggestion> repository,
            IRedirectsService redirectsService)
        {
            _suggestionLoader = suggestionLoader;
            _repository = repository;
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

        private void SaveRedirect(SuggestionRedirect suggestionRedirect)
        {
            var customRedirect = new CustomRedirect(suggestionRedirect.OldUrl,
                                                    suggestionRedirect.NewUrl,
                                                    false,
                                                    RedirectType.Permanent);

            _redirectsService.AddOrUpdate(customRedirect);
        }

        private void DeleteSuggestionsFor(string oldUrl)
        {
            var worker = DataAccessBaseEx.GetWorker();
            worker.DeleteSuggestionsForRequest(oldUrl);
        }
    }
}
