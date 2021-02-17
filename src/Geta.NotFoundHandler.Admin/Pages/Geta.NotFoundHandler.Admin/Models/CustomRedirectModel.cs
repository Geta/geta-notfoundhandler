using System;
using Geta.NotFoundHandler.Core.Redirects;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models
{
    public class CustomRedirectModel
    {
        public Guid? Id { get; set; }
        public string OldUrl { get; set; }
        public string NewUrl { get; set; }
        public bool WildCardSkipAppend { get; set; }
        public RedirectType RedirectType { get; set; }
    }
}
