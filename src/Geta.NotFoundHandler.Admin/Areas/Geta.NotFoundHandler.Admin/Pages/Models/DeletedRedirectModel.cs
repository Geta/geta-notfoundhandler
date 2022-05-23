using System.ComponentModel.DataAnnotations;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models
{
    public class DeletedRedirectModel
    {
        [Required]
        public string OldUrl { get; set; }
    }
}
