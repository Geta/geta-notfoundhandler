using Geta.NotFoundHandler.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin;

public abstract class BaseRedirectPageModel : PageModel
{
    public string Message { get; set; }

    public string OperationMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public QueryParams Params { get; set; }

    public void OnGet()
    {
        Load();
    }

    protected virtual IActionResult LoadPage(bool clearModelState = false)
    {
        if (clearModelState)
        {
            ModelState.Clear();
        }

        Load();
        return Page();
    }

    protected abstract void Load();
}
