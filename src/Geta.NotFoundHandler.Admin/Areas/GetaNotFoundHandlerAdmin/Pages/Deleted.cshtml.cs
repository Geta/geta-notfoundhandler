using System.Linq;
using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using X.PagedList;
using X.PagedList.Extensions;

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

    public IPagedList<CustomRedirect> Items { get; set; } = Enumerable.Empty<CustomRedirect>().ToPagedList();

    [BindProperty]
    public DeletedRedirectModel DeletedRedirect { get; set; }

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
        var items = _redirectsService.GetDeleted().ToPagedList(Paging.PageNumber, Paging.PageSize);
        Message =
            $"There are currently {items.TotalItemCount} URLs that return a Deleted response. This tells crawlers to remove these URLs from their index.";
        Items = items;
    }
}
