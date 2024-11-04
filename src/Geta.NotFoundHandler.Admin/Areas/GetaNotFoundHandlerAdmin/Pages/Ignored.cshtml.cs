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
public class IgnoredModel : AbstractSortablePageModel
{
    private readonly IRedirectsService _redirectsService;

    public IgnoredModel(IRedirectsService redirectsService)
    {
        _redirectsService = redirectsService;
    }

    public string Message { get; set; }

    public IPagedList<CustomRedirect> Items { get; set; } = Enumerable.Empty<CustomRedirect>().ToPagedList();

    [BindProperty(SupportsGet = true)]
    public Paging Paging { get; set; }

    public void OnGet(string sortColumn, SortDirection sortDirection)
    {
        ApplySort(sortColumn, sortDirection);

        Load();
    }

    public IActionResult OnPostUnignore(string oldUrl)
    {
        _redirectsService.DeleteByOldUrl(oldUrl);

        return RedirectToPage();
    }

    private void Load()
    {
        var items = FindRedirects().ToPagedList(Paging.PageNumber, Paging.PageSize);
        Message = $"There are currently {items.TotalItemCount} ignored suggestions stored.";
        Items = items;
    }

    private IEnumerable<CustomRedirect> FindRedirects()
    {
        var result = _redirectsService.GetIgnored();

        result = Sort(result);

        return result;
    }
}
