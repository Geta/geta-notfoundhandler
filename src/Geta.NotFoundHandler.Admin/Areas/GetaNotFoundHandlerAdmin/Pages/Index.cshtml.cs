using System.Collections.Generic;
using System.Linq;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Base;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Extensions;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Models;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages;
using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using X.PagedList;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin;

[Authorize(Constants.PolicyName)]
public class IndexModel : AbstractSortablePageModel
{
    private readonly IRedirectsService _redirectsService;

    public IndexModel(IRedirectsService redirectsService)
    {
        _redirectsService = redirectsService;
    }

    public string Message { get; set; }

    public IPagedList<CustomRedirect> Items { get; set; } = Enumerable.Empty<CustomRedirect>().ToPagedList();

    [BindProperty]
    public RedirectModel CustomRedirect { get; set; }

    [BindProperty(SupportsGet = true)]
    public Paging Paging { get; set; }

    [BindProperty(SupportsGet = true, Name = "q")]
    public string Query { get; set; }

    public bool HasQuery => !string.IsNullOrEmpty(Query);

    public void OnGet(RedirectsRequest request, string sortColumn, SortDirection? sortDirection)
    {
        ApplyRequest(request);
        ApplySort(sortColumn, sortDirection);

        Load();
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

            return RedirectToPage();
        }

        Load();
        return Page();
    }

    public IActionResult OnPostDelete(RedirectsRequest request)
    {
        ModelState.Clear();

        if (request.Id != null)
        {
            _redirectsService.DeleteById(request.Id.Value);
        }

        ApplyRequest(request);

        Load();

        return RedirectToPage(request);
    }

    public IActionResult OnPostUpdate(RedirectsRequest request)
    {
        if (ModelState.IsValid &&
            CustomRedirect.Id != null)
        {
            _redirectsService.AddOrUpdate(new CustomRedirect
            {
                Id = CustomRedirect.Id,
                OldUrl = CustomRedirect.OldUrl,
                RedirectType = CustomRedirect.RedirectType,
                NewUrl = CustomRedirect.NewUrl,
                WildCardSkipAppend = CustomRedirect.WildCardSkipAppend
            });

            return RedirectToPage(request);
        }

        Load();
        return Page();
    }

    private void Load()
    {
        var items = FindRedirects().ToPagedList(Paging.PageNumber, Paging.PageSize);
        Message = $"There are currently stored {items.TotalItemCount} custom redirects.";
        Items = items;
    }

    private IEnumerable<CustomRedirect> FindRedirects()
    {
        var result = HasQuery ? _redirectsService.Search(Query) : _redirectsService.GetSaved();

        return result
            .Sort(SortColumn, SortDirection);
    }

    private void ApplyRequest(RedirectsRequest request)
    {
        if (request.PageNumber.HasValue)
        {
            Paging.PageNumber = request.PageNumber.Value;
        }

        Query = request.Query ?? Query;
    }
}
