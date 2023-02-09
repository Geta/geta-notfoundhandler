using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin;

[Authorize(Constants.PolicyName)]
public class IndexModel : BaseCustomRedirectPageModel
{
    public IndexModel(IRedirectsService redirectsService) : base(redirectsService, RedirectState.Saved,
        "There are currently stored {0} custom redirects. ")
    {
    }

    [BindProperty]
    public RedirectModel CustomRedirect { get; set; }

    public IActionResult OnPostCreate()
    {
        if (ModelState.IsValid)
        {
            var customRedirect = new CustomRedirect(CustomRedirect.OldUrl,
                                                    CustomRedirect.NewUrl,
                                                    CustomRedirect.WildCardSkipAppend,
                                                    CustomRedirect.RedirectType);

            RedirectsService.AddOrUpdate(customRedirect);
            OperationMessage = $"Added/updated redirect from {CustomRedirect.OldUrl} to {CustomRedirect.NewUrl}";
            CustomRedirect = new RedirectModel();
            return LoadPage(true);
        }

        return LoadPage();
    }

    public IActionResult OnPostDelete(string oldUrl)
    {
        RedirectsService.DeleteByOldUrl(oldUrl);
        OperationMessage = $"Removed redirect for {oldUrl}";
        return LoadPage(true);
    }
}
