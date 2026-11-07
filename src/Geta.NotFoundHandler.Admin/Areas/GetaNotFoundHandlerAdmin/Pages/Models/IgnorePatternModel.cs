using System.ComponentModel.DataAnnotations;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models
{
    public class IgnorePatternModel
    {
        [Required]
        [StringLength(2000)]
        public string Pattern { get; set; }
    }
}
