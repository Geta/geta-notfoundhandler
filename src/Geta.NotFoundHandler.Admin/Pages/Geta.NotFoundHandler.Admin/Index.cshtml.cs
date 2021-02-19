using System;
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
        private readonly IRedirectsService _redirectsService;

        public IndexModel(IRedirectsService redirectsService)
        {
            _redirectsService = redirectsService;
        }

        public string Message { get; set; }

        public IPagedList<CustomRedirect> Items { get; set; } = Enumerable.Empty<CustomRedirect>().ToPagedList();

        [BindProperty]
        public RedirectModel Redirect { get; set; }

        [BindProperty(SupportsGet = true)]
        public Paging Paging { get; set; }
        
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

            var customRedirect = new CustomRedirect(Redirect.OldUrl,
                                                    Redirect.NewUrl,
                                                    Redirect.WildCardSkipAppend,
                                                    Redirect.RedirectType);

            _redirectsService.AddOrUpdate(customRedirect);

            return RedirectToPage();
        }

        public IActionResult OnPostDelete(string oldUrl)
        {
            _redirectsService.DeleteByOldUrl(oldUrl);
            return RedirectToPage();
        }

        private void Load()
        {
            var items = _redirectsService.GetSaved().ToPagedList(Paging.PageNumber, Paging.PageSize);
            Message = $"There are currently stored {items.TotalItemCount} custom redirects.";
            Items = items;
        }
    }
}
