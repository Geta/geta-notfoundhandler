using System;
using System.Collections.Generic;
using System.Linq;
using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models;
using Geta.NotFoundHandler.Core.Providers.RegexRedirects;
using Geta.NotFoundHandler.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Geta.NotFoundHandler.Admin.Areas.Geta.NotFoundHandler.Admin;

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

    private string EditItemId { get; set; }

    [BindProperty]
    public RegexRedirectModel RegexRedirect { get; set; }

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
        RegexRedirect = new RegexRedirectModel { OrderNumber = items.Select(x => x.OrderNumber).Max() + 1 };
    }

    private IList<RegexRedirect> FindRedirects()
    {
        return _redirectLoader.GetAll().ToList();
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
