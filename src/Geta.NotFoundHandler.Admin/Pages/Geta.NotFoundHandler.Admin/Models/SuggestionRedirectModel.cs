using System.ComponentModel.DataAnnotations;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models
{
    public class SuggestionRedirectModel
    {
        [Required]
        public string OldUrl { get; set; }
        [Required]
        public string NewUrl { get; set; }
        public int Count { get; set; }
    }
}
