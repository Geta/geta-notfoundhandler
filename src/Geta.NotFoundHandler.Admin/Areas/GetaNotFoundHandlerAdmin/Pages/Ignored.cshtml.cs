using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin;

[Authorize(Constants.PolicyName)]
public class IgnoredModel : PageModel
{
    private readonly IRedirectsService _redirectsService;

    public IgnoredModel(IRedirectsService redirectsService)
    {
        _redirectsService = redirectsService;
    }

    public string Message { get; set; }

    public CustomRedirectsResult Results { get; set; }

    [BindProperty(SupportsGet = true)]
    public QueryParams Params { get; set; }

    public void OnGet()
    {
        Load();
    }

    public IActionResult OnPostUnignore(string oldUrl)
    {
        _redirectsService.DeleteByOldUrl(oldUrl);

        return RedirectToPage();
    }

    private void Load()
    {
        Params.QueryState = RedirectState.Ignored;
        Params.PageSize ??= 50;
        var results = _redirectsService.GetRedirects(Params);
        Message = $"There are currently {results.UnfilteredCount} ignored suggestions stored. ";
        if (results.TotalCount < results.UnfilteredCount)
        {
            Message += $"Current filter gives {results.TotalCount}.";
        }
        Results = results;
    }
}
