using System;
using System.ComponentModel.DataAnnotations;
using Geta.NotFoundHandler.Core.Redirects;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models
{
    public class RedirectModel
    {
        public Guid? Id { get; set; }
        [Required]
        public string OldUrl { get; set; }
        [Required]
        public string NewUrl { get; set; }
        public bool WildCardSkipAppend { get; set; }
        public RedirectType RedirectType { get; set; }
    }
}
