using System;
using System.Collections.Generic;
using System.Linq;
using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models;
using Geta.NotFoundHandler.Core.Providers.RegexRedirects;
using Geta.NotFoundHandler.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Geta.NotFoundHandler.Admin.Areas.Geta.NotFoundHandler.Admin
{
    public class RegexModel : PageModel
    {
        private readonly IRegexRedirectsService _regexRedirectsService;
        private readonly IRegexRedirectLoader _redirectLoader;

        public RegexModel(
            IRegexRedirectsService regexRedirectsService,
            IRegexRedirectLoader redirectLoader)
        {
            _regexRedirectsService = regexRedirectsService;
            _redirectLoader = redirectLoader;
        }

        public string Message { get; set; }

        public IEnumerable<RegexRedirect> Items { get; set; } = Enumerable.Empty<RegexRedirect>();

        [BindProperty]
        public RegexRedirectModel RegexRedirect { get; set; }

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

            _regexRedirectsService.Create(RegexRedirect.OldUrlRegex, RegexRedirect.NewUrlFormat, RegexRedirect.OrderNumber);

            return RedirectToPage();
        }

        public IActionResult OnPostDelete(Guid id)
        {
            _regexRedirectsService.Delete(id);

            return RedirectToPage();
        }

        private void Load()
        {
            var items = FindRedirects();
            Message = $"There are currently stored {items.Count()} custom Regex redirects.";
            Items = items;
        }

        private IList<RegexRedirect> FindRedirects()
        {
            return _redirectLoader.GetAll().ToList();
        }
    }
}
