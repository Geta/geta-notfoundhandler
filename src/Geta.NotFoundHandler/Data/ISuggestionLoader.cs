using System.Collections.Generic;
using Geta.NotFoundHandler.Core.Suggestions;

namespace Geta.NotFoundHandler.Data
{
    public interface ISuggestionLoader
    {
        IEnumerable<SuggestionSummary> GetAllSummaries();
    }
}
