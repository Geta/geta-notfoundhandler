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

    private string EditItemId { get; set; }

    [BindProperty]
    public RegexRedirectModel RegexRedirect { get; set; }

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

        _regexRedirectsService.Create(RegexRedirect.OldUrlRegex, RegexRedirect.NewUrlFormat, RegexRedirect.OrderNumber);

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(Guid id)
    {
        _regexRedirectsService.Delete(id);

        return RedirectToPage();
    }

    public IActionResult OnPostEdit(Guid id)
    {
        ModelState.Clear();

        Load();

        EditItemId = id.ToString();

        var redirect = _redirectLoader.Get(id);

        RegexRedirect = new RegexRedirectModel
        {
            Id = redirect.Id,
            OldUrlRegex = redirect.OldUrlRegex.ToString(),
            NewUrlFormat = redirect.NewUrlFormat,
            OrderNumber = redirect.OrderNumber
        };

        return Page();
    }

    public IActionResult OnPostUpdate(Guid id)
    {
        if (!ModelState.IsValid)
        {
            Load();
            EditItemId = id.ToString();

            return Page();
        }

        _regexRedirectsService.Update(id,
                                      RegexRedirect.OldUrlRegex,
                                      RegexRedirect.NewUrlFormat,
                                      RegexRedirect.OrderNumber);
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

    public bool IsEditing(Guid? id)
    {
        return id.ToString() == EditItemId;
    }

    public bool IsEditing()
    {
        return !string.IsNullOrEmpty(EditItemId);
    }
}
