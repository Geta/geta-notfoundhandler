using System;
using System.Collections.Generic;
using System.Linq;
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
public class RegexModel : PageModel
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

    [BindProperty]
    public RegexRedirectModel EditRedirect { get; set; }

    public void OnGet()
    {
        Load();
    }

    public IActionResult OnPostCreate()
    {
        if (ModelState.GetValidationState($"{nameof(RegexRedirect)}.{nameof(RegexRedirect.OldUrlRegex)}") ==
            ModelValidationState.Valid &&
            ModelState.GetValidationState($"{nameof(RegexRedirect)}.{nameof(RegexRedirect.NewUrlFormat)}") ==
            ModelValidationState.Valid)
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
        if (ModelState.GetValidationState($"{nameof(EditRedirect)}.{nameof(EditRedirect.OldUrlRegex)}") ==
            ModelValidationState.Valid &&
            ModelState.GetValidationState($"{nameof(EditRedirect)}.{nameof(EditRedirect.NewUrlFormat)}") ==
            ModelValidationState.Valid &&
            EditRedirect.Id != null)
        {
            _regexRedirectsService.Update(EditRedirect.Id.Value,
                                          EditRedirect.OldUrlRegex,
                                          EditRedirect.NewUrlFormat,
                                          EditRedirect.OrderNumber);
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

    private IList<RegexRedirect> FindRedirects()
    {
        return _redirectLoader.GetAll().ToList();
    }
}
