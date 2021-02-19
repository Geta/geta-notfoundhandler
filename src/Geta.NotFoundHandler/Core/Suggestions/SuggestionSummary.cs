using System.Collections.Generic;

namespace Geta.NotFoundHandler.Core.Suggestions
{
    public class SuggestionSummary
    {
        public string OldUrl { get; set; }
        public int Count { get; set; }
        public ICollection<RefererSummary> Referers { get; set; }
    }
}
