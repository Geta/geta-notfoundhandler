using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Base;

public abstract class AbstractSortablePageModel : PageModel
{
    public string SortColumn { get; set; }
    public SortDirection? SortDirection { get; set; }

    public void ApplySort(string sortColumn, SortDirection? sortDirection)
    {
        SortColumn = sortColumn;
        SortDirection = sortDirection;
    }
}
