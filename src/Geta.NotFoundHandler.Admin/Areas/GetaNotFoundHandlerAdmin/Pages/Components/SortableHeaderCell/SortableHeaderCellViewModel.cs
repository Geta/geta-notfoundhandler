using System;
using System.Web;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Base;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Models;

namespace Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Components.SortableHeaderCell;

public class SortableHeaderCellViewModel
{
    public string DisplayName { get; set; }
    public string Key { get; set; }
    public string QueryString { get; set; }

    public SortDirection? GetNextSortDirection()
    {
        return GetSortDirection() switch
        {
            null => SortDirection.Ascending,
            SortDirection.Ascending => SortDirection.Descending,
            SortDirection.Descending => null,
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
    
    public string GetSortUrl()
    {
        var qs = HttpUtility
            .ParseQueryString(QueryString);

        qs[nameof(AbstractSortablePageModel.SortColumn)] = GetSortColumn();
        qs[nameof(AbstractSortablePageModel.SortDirection)] = GetNextSortDirection().ToString();

        return $"?{qs}";
    }

    public bool IsActive()
    {
        var sortColumn = HttpUtility
            .ParseQueryString(QueryString)
            .Get(nameof(AbstractSortablePageModel.SortColumn));

        return !string.IsNullOrEmpty(sortColumn) && sortColumn == Key;
    }

    public SortDirection? GetSortDirection()
    {
        var sortDirection = HttpUtility
            .ParseQueryString(QueryString)
            .Get(nameof(AbstractSortablePageModel.SortDirection));

        return Enum.TryParse(sortDirection, out SortDirection sort) ? sort : null;
    }
}
