using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Base;

public abstract class AbstractAdminPageModel : PageModel
{
    private const int MaxConfirmationLength = 300;

    private string _confirmation;

    /// <summary>
    /// What the last post did. It's kept in TempData as the page redirects after a post.
    /// </summary>
    /// <remarks>
    /// TempData is a cookie sent with every request to the site, and a message can hold a url from a request
    /// to the site. So it's limited, as a cookie too large for the server would lock the browser out of the site.
    /// </remarks>
    [TempData]
    public string Confirmation
    {
        get => _confirmation;
        set => _confirmation = value?.Length > MaxConfirmationLength ? value[..MaxConfirmationLength] + "..." : value;
    }
}
