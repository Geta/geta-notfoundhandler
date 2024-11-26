using System;
using System.Collections.Generic;
using System.Linq;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Base;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Extensions;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Models;
using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models;
using Geta.NotFoundHandler.Core.Providers.RegexRedirects;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Geta.NotFoundHandler.Admin.Areas.Geta.NotFoundHandler.Admin;

[Authorize(Constants.PolicyName)]
public class RegexModel : AbstractSortablePageModel
{
    private readonly IRegexRedirectLoader _redirectLoader;
    private readonly IRegexRedirectsService _regexRedirectsService;

    public RegexModel(
        IRegexRedirectsService regexRedirectsService,
        IRegexRedirectLoader redirectLoader)
    {
        _regexRedirectsService = regexRedirectsService;
        _redirectLoader = redirectLoader;
    }

    public string Message { get; set; }

    public IEnumerable<RegexRedirect> Items { get; set; } = Enumerable.Empty<RegexRedirect>();

    [BindProperty]
    public RegexRedirectModel RegexRedirect { get; set; }

    public void OnGet(string sortColumn, SortDirection? sortDirection)
    {
        ApplySort(sortColumn, sortDirection);

        Load();
    }

    public IActionResult OnPostCreate()
    {
        if (ModelState.IsValid)
        {
            _regexRedirectsService.Create(RegexRedirect.OldUrlRegex, RegexRedirect.NewUrlFormat, RegexRedirect.OrderNumber);

            return RedirectToPage();

        }

        Load();
        return Page();
    }

    public IActionResult OnPostDelete(Guid id)
    {
        _regexRedirectsService.Delete(id);

        return RedirectToPage();
    }

    public IActionResult OnPostUpdate()
    {
        if (ModelState.IsValid &&
            RegexRedirect.Id != null)
        {
            _regexRedirectsService.Update(RegexRedirect.Id.Value,
                                          RegexRedirect.OldUrlRegex,
                                          RegexRedirect.NewUrlFormat,
                                          RegexRedirect.OrderNumber);
            return RedirectToPage();
        }

        Load();

        return Page();
    }
    
    public IActionResult OnPostCancel(Guid id)
    {
        return RedirectToPage();
    }

    private void Load()
    {
        var items = FindRedirects();
        Message = $"There are currently stored {items.Count()} Regex redirects.";
        Items = items;
        RegexRedirect = new RegexRedirectModel { OrderNumber = items.Select(x => x.OrderNumber).DefaultIfEmpty().Max() + 1 };
    }

    private IEnumerable<RegexRedirect> FindRedirects()
    {
        return _redirectLoader
            .GetAll()
            .Sort(SortColumn, SortDirection);
    }
}
