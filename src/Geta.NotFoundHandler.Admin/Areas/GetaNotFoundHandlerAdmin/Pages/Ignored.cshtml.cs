using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin;

[Authorize(Constants.PolicyName)]
public class IgnoredModel : BaseCustomRedirectPageModel
{
    public IgnoredModel(IRedirectsService redirectsService) : base(redirectsService, RedirectState.Ignored,
        "There are currently {0} ignored suggestions stored. ")
    {
    }

    public IActionResult OnPostUnignore(string oldUrl)
    {
        RedirectsService.DeleteByOldUrl(oldUrl);
        OperationMessage = $"Removed {oldUrl} from the ignore list";
        return LoadPage(true);
    }
}
