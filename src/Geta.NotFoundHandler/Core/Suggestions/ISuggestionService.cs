using System.Collections.Generic;

namespace Geta.NotFoundHandler.Core.Suggestions
{
    public interface ISuggestionService
    {
        IEnumerable<SuggestionSummary> GetAllSummaries();
        void AddRedirect(SuggestionRedirect suggestionRedirect);
        void IgnoreSuggestion(string oldUrl);
        void DeleteAll();
        void Delete(int maxErrors, int minimumDays);
    }
}
