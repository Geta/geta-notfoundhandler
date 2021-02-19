using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Geta.NotFoundHandler.Core.Suggestions;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models
{
    public class SuggestionRedirectModel
    {
        [Required]
        public string OldUrl { get; set; }
        [Required]
        public string NewUrl { get; set; }
        public int Count { get; set; }
        public ICollection<RefererSummary> Referers { get; set; } = new List<RefererSummary>();
    }
}
