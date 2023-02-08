using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin;

[Authorize(Constants.PolicyName)]
public class DeletedModel : BaseCustomRedirectPageModel
{
    public DeletedModel(IRedirectsService redirectsService) : base(redirectsService, RedirectState.Deleted,
        "There are currently {0} URLs that return a Deleted response.This tells crawlers to remove these URLs from their index. ")
    {
    }

    [BindProperty]
    public DeletedRedirectModel DeletedRedirect { get; set; }

    public IActionResult OnPostCreate()
    {
        if (ModelState.IsValid)
        {
            RedirectsService.AddDeletedRedirect(DeletedRedirect.OldUrl);
        }

        return LoadPage();
    }

    public IActionResult OnPostDelete(string oldUrl)
    {
        RedirectsService.DeleteByOldUrl(oldUrl);
        return LoadPage(true);
    }
}
