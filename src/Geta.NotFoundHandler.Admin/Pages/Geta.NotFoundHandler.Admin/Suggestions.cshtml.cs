using System.Collections.Generic;
using System.Linq;
using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models;
using Geta.NotFoundHandler.Core.Suggestions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using X.PagedList;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin
{
    public class SuggestionsModel : PageModel
    {
        private readonly ISuggestionService _suggestionService;

        public SuggestionsModel(ISuggestionService suggestionService)
        {
            _suggestionService = suggestionService;
        }

        public string Message { get; set; }

        public IPagedList<SuggestionRedirectModel> Items { get; set; } = Enumerable.Empty<SuggestionRedirectModel>().ToPagedList();

        [BindProperty(SupportsGet = true)]
        public Paging Paging { get; set; }

        public void OnGet()
        {
            Load();
        }

        public IActionResult OnPostCreate(Dictionary<int, SuggestionRedirectModel> items)
        {
            if (!ModelState.IsValid)
            {
                Load();
                return Page();
            }

            var item = items.First().Value;

            _suggestionService.AddRedirect(new SuggestionRedirect(item.OldUrl, item.NewUrl));
            
            return RedirectToPage();
        }

        public IActionResult OnPostIgnore(string oldUrl)
        {
            _suggestionService.IgnoreSuggestion(oldUrl);

            return RedirectToPage();
        }

        private void Load()
        {
            var items = _suggestionService.GetAllSummaries().Select(x => new SuggestionRedirectModel
            {
                OldUrl = x.OldUrl,
                Count = x.Count,
                Referers = x.Referers
            }).ToPagedList(Paging.PageNumber, Paging.PageSize);
            Message = $"Based on the logged 404 errors, there are {items.TotalItemCount} custom redirect suggestions.";
            Items = items;
        }
    }
}
