// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using X.PagedList;

namespace Geta.NotFoundHandler.Core.Suggestions
{
    public class DefaultSuggestionService : ISuggestionService
    {
        private readonly ISuggestionLoader _suggestionLoader;
        private readonly IRedirectsService _redirectsService;
        private readonly ISuggestionRepository _suggestionRepository;
        private readonly NotFoundHandlerOptions _options;

        public DefaultSuggestionService(
            ISuggestionLoader suggestionLoader,
            IRedirectsService redirectsService,
            ISuggestionRepository suggestionRepository,
            IOptions<NotFoundHandlerOptions> options)
        {
            _suggestionLoader = suggestionLoader;
            _redirectsService = redirectsService;
            _suggestionRepository = suggestionRepository;
            _options = options.Value;
        }

        public IPagedList<SuggestionSummary> GetSummaries(int page, int pageSize)
        {
            return _suggestionLoader.GetSummaries(page, pageSize);
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
            var customRedirect = new CustomRedirect(suggestionRedirect.OldUrl, suggestionRedirect.NewUrl, false, _options.DefaultRedirectType);
            _redirectsService.AddOrUpdate(customRedirect);
        }

        private void DeleteSuggestionsFor(string oldUrl)
        {
            _suggestionRepository.DeleteForRequest(oldUrl);
        }
    }
}
