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
public class IndexModel : PageModel
{
    private readonly IRedirectsService _redirectsService;

    public IndexModel(IRedirectsService redirectsService)
    {
        _redirectsService = redirectsService;
    }

    public string Message { get; set; }

    public CustomRedirectsResult Results { get; set; }

    [BindProperty]
    public RedirectModel CustomRedirect { get; set; }

    [BindProperty(SupportsGet = true)]
    public QueryParams Params { get; set; }

    public void OnGet()
    {
        Load();
    }

    public IActionResult OnPost()
    {
        ModelState.Clear();
        return LoadPage();
    }

    public IActionResult OnPostCreate()
    {
        if (ModelState.IsValid)
        {
            var customRedirect = new CustomRedirect(CustomRedirect.OldUrl,
                                                    CustomRedirect.NewUrl,
                                                    CustomRedirect.WildCardSkipAppend,
                                                    CustomRedirect.RedirectType);

            _redirectsService.AddOrUpdate(customRedirect);
        }

        return LoadPage();
    }

    public IActionResult OnPostDelete(string oldUrl)
    {
        _redirectsService.DeleteByOldUrl(oldUrl);
        return LoadPage();
    }

    public IActionResult LoadPage()
    {
        Load();
        return Page();
    }

    private void Load()
    {
        Params.QueryState = RedirectState.Saved;
        Params.PageSize ??= 50;
        var results = _redirectsService.GetRedirects(Params);
        Message = $"There are currently stored {results.UnfilteredCount} custom redirects. ";
        if (results.TotalCount < results.UnfilteredCount)
        {
            Message += $"Current filter gives {results.TotalCount}.";
        }
        Results = results;
    }
}
