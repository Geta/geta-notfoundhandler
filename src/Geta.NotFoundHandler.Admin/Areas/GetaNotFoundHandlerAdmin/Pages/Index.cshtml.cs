using System.Collections.Generic;
using System.Linq;
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
public class IndexModel : PageModel
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

    [BindProperty]
    public RedirectModel EditRedirect { get; set; }

    public void OnGet(RedirectsRequest request)
    {
        ApplyRequest(request);

        Load();
    }

    public IActionResult OnPostCreate()
    {
        if (ModelState.GetValidationState($"{nameof(CustomRedirect)}.{nameof(CustomRedirect.OldUrl)}") ==
            ModelValidationState.Valid &&
            ModelState.GetValidationState($"{nameof(CustomRedirect)}.{nameof(CustomRedirect.NewUrl)}") ==
            ModelValidationState.Valid)
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

        _redirectsService.DeleteById(request.Id);

        ApplyRequest(request);

        Load();

        return RedirectToPage(request);
    }

    public IActionResult OnPostUpdate(RedirectsRequest request)
    {
        if (ModelState.GetValidationState($"{nameof(EditRedirect)}.{nameof(EditRedirect.OldUrl)}") ==
            ModelValidationState.Valid &&
            ModelState.GetValidationState($"{nameof(EditRedirect)}.{nameof(EditRedirect.NewUrl)}") ==
            ModelValidationState.Valid &&
            EditRedirect.Id != null)
        {
            _redirectsService.AddOrUpdate(new CustomRedirect
            {
                Id = EditRedirect.Id,
                OldUrl = EditRedirect.OldUrl,
                RedirectType = EditRedirect.RedirectType,
                NewUrl = EditRedirect.NewUrl,
                WildCardSkipAppend = EditRedirect.WildCardSkipAppend
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
        return HasQuery ? _redirectsService.Search(Query) : _redirectsService.GetSaved();
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
