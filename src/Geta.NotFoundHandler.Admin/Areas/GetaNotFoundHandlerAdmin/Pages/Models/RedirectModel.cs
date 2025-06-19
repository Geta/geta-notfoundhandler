using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Geta.NotFoundHandler.Core.Redirects;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models
{
    public class RedirectModel : IValidatableObject
    {
        public Guid? Id { get; set; }
        
        [Required]
        public string OldUrl { get; set; }
        
        [Required]
        public string NewUrl { get; set; }
        
        public bool WildCardSkipAppend { get; set; }
        
        public RedirectType RedirectType { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Uri.IsWellFormedUriString(OldUrl, UriKind.RelativeOrAbsolute))
            {
                yield return new ValidationResult("Old Url must be a valid absolute or relative url.", new[] { nameof(OldUrl) });
            }

            if (!Uri.IsWellFormedUriString(NewUrl, UriKind.RelativeOrAbsolute))
            {
                yield return new ValidationResult("New Url must be a valid absolute or relative url.", new[] { nameof(NewUrl) });
            }
        }
    }
}
