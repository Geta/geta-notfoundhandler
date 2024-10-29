using System;
using System.Collections.Generic;
using System.Linq;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages;
using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    private Guid? EditItemId { get; set; }
    
    public void OnGet(RedirectsRequest request)
    {
        ApplyRequest(request);

        Load();
    }

    public IActionResult OnPostCreate()
    {
        if (!ModelState.IsValid)
        {
            Load();
            return Page();
        }

        var customRedirect = new CustomRedirect(CustomRedirect.OldUrl,
                                                CustomRedirect.NewUrl,
                                                CustomRedirect.WildCardSkipAppend,
                                                CustomRedirect.RedirectType);

        _redirectsService.AddOrUpdate(customRedirect);

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(RedirectsRequest request)
    {
        ModelState.Clear();

        _redirectsService.DeleteById(request.Id);
        
        ApplyRequest(request);

        Load();
        
        return RedirectToPage(request);
    }

    public IActionResult OnPostEdit(RedirectsRequest request)
    {
        ModelState.Clear();

        ApplyRequest(request);

        Load();

        var redirect = _redirectsService.Get(request.Id);

        if (redirect != null)
        {
            ActivateEditMode(request.Id);

            CustomRedirect = new RedirectModel
            {
                OldUrl = redirect.OldUrl,
                RedirectType = redirect.RedirectType,
                NewUrl = redirect.NewUrl,
                WildCardSkipAppend = redirect.WildCardSkipAppend
            };
        }

        return Page();
    }
    
    public IActionResult OnPostUpdate(RedirectsRequest request)
    {
        if (!ModelState.IsValid)
        {
            ApplyRequest(request);

            Load();

            ActivateEditMode(request.Id);

            return Page();
        }

        _redirectsService.AddOrUpdate(new CustomRedirect
        {
            Id = request.Id,
            OldUrl = CustomRedirect.OldUrl,
            RedirectType = CustomRedirect.RedirectType,
            NewUrl = CustomRedirect.NewUrl,
            WildCardSkipAppend = CustomRedirect.WildCardSkipAppend
        });

        return RedirectToPage(request);
    }
    
    public IActionResult OnPostCancelEdit(RedirectsRequest request)
    {
        return RedirectToPage(request);
    }
    
    public bool IsEditing(Guid? id)
    {
        return id.HasValue && id == EditItemId;
    }
    
    public bool IsEditing()
    {
        return EditItemId != null;
    }

    private void ActivateEditMode(Guid id)
    {
        EditItemId = id;
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
