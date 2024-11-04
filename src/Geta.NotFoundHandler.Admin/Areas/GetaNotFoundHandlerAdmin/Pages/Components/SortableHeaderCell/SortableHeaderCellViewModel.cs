using System;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Models;

namespace Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Components.SortableHeaderCell;

public class SortableHeaderCellViewModel
{
    public string DisplayName { get; set; }
    public string Key { get; set; }
    public SortDirection? SortDirection { get; set; }
    public bool IsActive { get; set; }

    public SortDirection? GetNextSortDirection()
    {
        return SortDirection switch
        {
            null => Models.SortDirection.Ascending,
            Models.SortDirection.Ascending => Models.SortDirection.Descending,
            Models.SortDirection.Descending => null,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public string GetSortColumn()
    {
        if (GetNextSortDirection() == null)
        {
            return null;
        }

        return Key;
    }
}
