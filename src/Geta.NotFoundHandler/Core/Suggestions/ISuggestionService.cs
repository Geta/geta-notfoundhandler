// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;

namespace Geta.NotFoundHandler.Core.Suggestions
{
    public interface ISuggestionService
    {
        IEnumerable<SuggestionSummary> GetAllSummaries();
        IEnumerable<SuggestionSummary> GetSummariesPaged(int page, int pageSize);
        void AddRedirect(SuggestionRedirect suggestionRedirect);
        void IgnoreSuggestion(string oldUrl);
        void DeleteAll();
        void Delete(int maxErrors, int minimumDays);
    }
}
