// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Geta.NotFoundHandler.Data;

namespace Geta.NotFoundHandler.Core.Suggestions
{
    public interface ISuggestionService : ISuggestionLoader
    {
        void AddRedirect(SuggestionRedirect suggestionRedirect);

        void IgnoreSuggestion(string oldUrl);

        void DeleteAll();

        void Delete(int maxErrors, int minimumDays);
    }
}
