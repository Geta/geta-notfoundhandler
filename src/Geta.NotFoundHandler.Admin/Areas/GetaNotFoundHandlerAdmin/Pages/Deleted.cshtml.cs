using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models;
using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin;

[Authorize(Constants.PolicyName)]
public class DeletedModel : PageModel
{
    private readonly IRedirectsService _redirectsService;

    public DeletedModel(IRedirectsService redirectsService)
    {
        _redirectsService = redirectsService;
    }

    public string Message { get; set; }

    public CustomRedirectsResult Results { get; set; }

    [BindProperty]
    public DeletedRedirectModel DeletedRedirect { get; set; }

    [BindProperty(SupportsGet = true)]
    public QueryParams Params { get; set; }

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

        _redirectsService.AddDeletedRedirect(DeletedRedirect.OldUrl);

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(string oldUrl)
    {
        _redirectsService.DeleteByOldUrl(oldUrl);
        return RedirectToPage();
    }

    private void Load()
    {
        Params.QueryState = RedirectState.Deleted;
        Params.PageSize ??= 50;
        var results = _redirectsService.GetRedirects(Params);
        Message =
            $"There are currently {results.UnfilteredCount} URLs that return a Deleted response. This tells crawlers to remove these URLs from their index.";

        if (results.TotalCount < results.UnfilteredCount)
        {
            Message += $"Current filter gives {results.TotalCount}.";
        }
        Results = results;
    }
}
