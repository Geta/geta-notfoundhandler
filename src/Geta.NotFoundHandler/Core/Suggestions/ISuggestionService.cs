// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using X.PagedList;

namespace Geta.NotFoundHandler.Core.Suggestions
{
    public interface ISuggestionService
    {
        IPagedList<SuggestionSummary> GetSummaries(int page, int pageSize);
        void AddRedirect(SuggestionRedirect suggestionRedirect);
        void IgnoreSuggestion(string oldUrl);
        void DeleteAll();
        void Delete(int maxErrors, int minimumDays);
    }
}
