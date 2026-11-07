using System;
using System.Collections.Generic;
using System.Linq;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Base;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Extensions;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Models;
using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Core.Suggestions;
using Geta.NotFoundHandler.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;
using X.PagedList.Extensions;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin;

[Authorize(Constants.PolicyName)]
public class IgnoredModel : AbstractSortablePageModel
{
    private readonly IRedirectsService _redirectsService;
    private readonly ISuggestionService _suggestionService;

    public IgnoredModel(IRedirectsService redirectsService, ISuggestionService suggestionService)
    {
        _redirectsService = redirectsService;
        _suggestionService = suggestionService;
    }

    public string Message { get; set; }

    public IPagedList<CustomRedirect> Items { get; set; } = Enumerable.Empty<CustomRedirect>().ToPagedList();

    [BindProperty]
    public IgnorePatternModel NewPattern { get; set; }

    [BindProperty(SupportsGet = true)]
    public Paging Paging { get; set; }

    public void OnGet(string sortColumn, SortDirection? sortDirection)
    {
        ApplySort(sortColumn, sortDirection);

        Load();
    }

    public IActionResult OnPostCreate()
    {
        var pattern = NewPattern?.Pattern?.Trim();

        if (ModelState.IsValid && !IsValidPattern(pattern))
        {
            ModelState.AddModelError(
                $"{nameof(NewPattern)}.{nameof(IgnorePatternModel.Pattern)}",
                "Enter a url starting with /, * or http, with a * in it and more than wildcards and slashes, such as /api/* or *.php.");
        }

        if (!ModelState.IsValid)
        {
            Load();
            return Page();
        }

        _suggestionService.IgnoreSuggestion(pattern);

        Confirmation = $"Ignoring suggestions for {pattern}.";

        return RedirectToPage();
    }

    public IActionResult OnPostUnignore(string oldUrl)
    {
        _redirectsService.DeleteByOldUrl(oldUrl);

        Confirmation = $"Removed {oldUrl} from the ignored suggestions.";

        return RedirectToPage();
    }

    private static bool IsValidPattern(string pattern)
    {
        // Note: Requests are matched by path or whole url, so anything else would never match.
        var hasValidStart = pattern.StartsWith('/')
                            || pattern.StartsWith('*')
                            || pattern.StartsWith("http", StringComparison.OrdinalIgnoreCase);

        return hasValidStart && IgnorePattern.IsPattern(pattern);
    }

    private void Load()
    {
        var items = FindRedirects().ToPagedList(Paging.PageNumber, Paging.PageSize);
        Message = $"There are currently {items.TotalItemCount} ignored suggestions stored.";
        Items = items;
    }

    private IEnumerable<CustomRedirect> FindRedirects()
    {
        return _redirectsService
            .GetIgnored()
            .Sort(SortColumn, SortDirection);
    }
}
