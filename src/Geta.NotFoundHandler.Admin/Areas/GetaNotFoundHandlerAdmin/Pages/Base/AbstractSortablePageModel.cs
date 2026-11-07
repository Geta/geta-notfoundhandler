using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Models;

namespace Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Base;

public abstract class AbstractSortablePageModel : AbstractAdminPageModel
{
    public string SortColumn { get; set; }
    public SortDirection? SortDirection { get; set; }

    public void ApplySort(string sortColumn, SortDirection? sortDirection)
    {
        SortColumn = sortColumn;
        SortDirection = sortDirection;
    }
}
