using System.ComponentModel.DataAnnotations;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models
{
    public class IgnorePatternModel
    {
        [Required]
        public string Pattern { get; set; }
    }
}
