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

    [BindProperty(Name = nameof(RegexRedirect))]
    public RegexRedirectModel RegexRedirect { get; set; }

    [BindProperty(Name = nameof(EditRedirect))]
    public RegexRedirectModel EditRedirect { get; set; }

    public void OnGet(string sortColumn, SortDirection? sortDirection)
    {
        ApplySort(sortColumn, sortDirection);

        Load();
    }

    public IActionResult OnPostCreate()
    {
        ModelState.RemoveNestedKeys(nameof(EditRedirect));

        if (ModelState.IsValid)
        {
            _regexRedirectsService.Create(RegexRedirect.OldUrlRegex,
                                          RegexRedirect.NewUrlFormat,
                                          RegexRedirect.OrderNumber,
                                          RegexRedirect.RedirectType);

            Confirmation = $"Saved the regex redirect from {RegexRedirect.OldUrlRegex} to {RegexRedirect.NewUrlFormat}.";

            return RedirectToPage();
        }

        Load();
        return Page();
    }

    public IActionResult OnPostDelete(Guid id)
    {
        _regexRedirectsService.Delete(id);

        Confirmation = "Deleted the regex redirect.";

        return RedirectToPage();
    }

    public IActionResult OnPostUpdate()
    {
        ModelState.RemoveNestedKeys(nameof(RegexRedirect));

        if (ModelState.IsValid &&
            EditRedirect.Id != null)
        {
            _regexRedirectsService.Update(EditRedirect.Id.Value,
                                          EditRedirect.OldUrlRegex,
                                          EditRedirect.NewUrlFormat,
                                          EditRedirect.OrderNumber,
                                          EditRedirect.RedirectType);

            Confirmation = $"Saved the regex redirect from {EditRedirect.OldUrlRegex} to {EditRedirect.NewUrlFormat}.";

            return RedirectToPage();
        }

        Load();

        return Page();
    }

    private void Load()
    {
        var items = FindRedirects();
        Message = $"There are currently stored {items.Count()} Regex redirects.";
        Items = items;
        RegexRedirect = new RegexRedirectModel
        {
            OrderNumber = items.Select(x => x.OrderNumber).DefaultIfEmpty().Max() + 1,
            // Note: A form coming back with a validation error keeps the type that was chosen.
            RedirectType = RegexRedirect?.RedirectType
        };
    }

    private IEnumerable<RegexRedirect> FindRedirects()
    {
        return _redirectLoader
            .GetAll()
            .Sort(SortColumn, SortDirection);
    }
}
