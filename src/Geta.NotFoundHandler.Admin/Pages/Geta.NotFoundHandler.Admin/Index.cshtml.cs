using System;
using System.Collections.Generic;
using System.Linq;
using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models;
using Geta.NotFoundHandler.Core.Redirects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using X.PagedList;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin
{
    public class IndexModel : PageModel
    {
        private readonly IRedirectsService _redirectService;

        public IndexModel(IRedirectsService redirectService)
        {
            _redirectService = redirectService;
        }

        public string Message { get; set; }

        public IPagedList<CustomRedirect> Items { get; set; } = Enumerable.Empty<CustomRedirect>().ToPagedList();

        [BindProperty]
        public CustomRedirectModel CustomRedirect { get; set; }

        [FromQuery(Name = "page")]
        public int PageNumber { get; set; } = 1;

        [FromQuery(Name = "page-size")]
        public int PageSize { get; set; } = 50;

        public void OnGet()
        {
            Load();
        }

        public IActionResult OnPostCreate()
        {
            if (!ModelState.IsValid)
            {
                Load();
                return Page();
            }

            var customRedirect = new CustomRedirect(CustomRedirect.OldUrl,
                                                    CustomRedirect.NewUrl,
                                                    CustomRedirect.WildCardSkipAppend,
                                                    CustomRedirect.RedirectType);

            _redirectService.AddOrUpdate(customRedirect);

            return RedirectToPage();
        }

        public IActionResult OnPostDeleteAsync(Guid id)
        {
            _redirectService.Delete(id);
            return RedirectToPage();
        }

        private void Load()
        {
            var redirects = _redirectService.GetSaved().ToList();
            var paged = redirects.ToPagedList(PageNumber, PageSize);
            Message = $"There are currently stored {redirects.Count} custom redirects.";
            Items = paged;
        }
    }
}
