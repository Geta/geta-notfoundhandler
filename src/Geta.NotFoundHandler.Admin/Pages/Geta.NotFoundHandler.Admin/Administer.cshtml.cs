using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Core.Suggestions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin
{
    public class AdministerModel : PageModel
    {
        private readonly IRedirectsService _redirectsService;
        private readonly ISuggestionService _suggestionService;

        public AdministerModel(IRedirectsService redirectsService, ISuggestionService suggestionService)
        {
            _redirectsService = redirectsService;
            _suggestionService = suggestionService;
        }

        public string Message { get; set; }

        [BindProperty]
        public DeleteSuggestionsModel DeleteSuggestions { get; set; } = new DeleteSuggestionsModel();

        public void OnGet()
        {
        }

        public IActionResult OnPostDeleteAllIgnoredSuggestions()
        {
            var count = _redirectsService.DeleteAllIgnored();
            Message = $"All {count} ignored suggestions permanently removed";
            return RedirectToPage();
        }

        public IActionResult OnPostDeleteAllSuggestions()
        {
            _suggestionService.DeleteAll();
            Message = "Suggestions successfully deleted";
            return RedirectToPage();
        }

        public IActionResult OnPostDeleteAllRedirects()
        {
            _redirectsService.DeleteAll();
            Message = "Redirects successfully deleted";
            return RedirectToPage();
        }

        public IActionResult OnPostDeleteSuggestions()
        {
            if (!ModelState.IsValid) return Page();

            _suggestionService.Delete(DeleteSuggestions.MaxErrors, DeleteSuggestions.MinimumDays);
            Message = "Suggestions successfully deleted";

            return RedirectToPage();
        }

        public void OnPostImportRedirects()
        {

        }

        public void OnPostImportDeletedRedirects()
        {

        }

        public void OnPostExportRedirects()
        {

        }
    }

    public class DeleteSuggestionsModel
    {
        public int MaxErrors { get; set; } = 5;
        public int MinimumDays { get; set; } = 30;
    }
}
