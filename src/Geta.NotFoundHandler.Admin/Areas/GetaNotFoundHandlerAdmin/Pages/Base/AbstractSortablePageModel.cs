using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Components.SortableHeaderCell;
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

    public SortableHeaderCellViewModel CreateHeaderCellViewModel(string displayName, string key)
    {
        return new SortableHeaderCellViewModel
        {
            DisplayName = displayName,
            Key = key,
            IsActive = IsActiveSortColumn(key),
            SortDirection = GetSortDirection(key)
        };
    }

    private SortDirection? GetSortDirection(string key)
    {
        if (IsActiveSortColumn(key))
        {
            return SortDirection ?? Models.SortDirection.Ascending;
        }

        return null;
    } 

    private bool IsActiveSortColumn(string key) => SortColumn == key;
}
