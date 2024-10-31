using System.Collections.Generic;
using System.Linq;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Base;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Models;
using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin;

[Authorize(Constants.PolicyName)]
public class DeletedModel : AbstractSortablePageModel
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

    public void OnGet(string sortColumn, SortDirection sortDirection)
    {
        ApplySort(sortColumn, sortDirection);

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
        var items = FindRedirects().ToPagedList(Paging.PageNumber, Paging.PageSize);
        Message =
            $"There are currently {items.TotalItemCount} URLs that return a Deleted response. This tells crawlers to remove these URLs from their index.";
        Items = items;
    }
    
    private List<CustomRedirect> FindRedirects()
    {
        var result = _redirectsService.GetDeleted();

        result = Sort(result);

        return result.ToList();
    }
}
