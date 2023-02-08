// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Data;

namespace Geta.NotFoundHandler.Core.Suggestions
{
    public class DefaultSuggestionService : ISuggestionService
    {
        private readonly ISuggestionLoader _suggestionLoader;
        private readonly IRedirectsService _redirectsService;
        private readonly ISuggestionRepository _suggestionRepository;

        public DefaultSuggestionService(
            ISuggestionLoader suggestionLoader,
            IRedirectsService redirectsService,
            ISuggestionRepository suggestionRepository)
        {
            _suggestionLoader = suggestionLoader;
            _redirectsService = redirectsService;
            _suggestionRepository = suggestionRepository;
        }

        public SuggestionRedirectsResult GetSummaries(QueryParams query)
        {
            return _suggestionLoader.GetSummaries(query);
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
            _suggestionRepository.DeleteAll();
        }

        public void Delete(int maxErrors, int minimumDays)
        {
            _suggestionRepository.Delete(maxErrors, minimumDays);
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
            _suggestionRepository.DeleteForRequest(oldUrl);
        }
    }
}
