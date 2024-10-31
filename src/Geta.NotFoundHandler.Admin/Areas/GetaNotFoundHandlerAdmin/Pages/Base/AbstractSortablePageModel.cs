using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Components.SortableHeaderCell;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Base;

public abstract class AbstractSortablePageModel : PageModel
{
    public string SortColumn { get; set; }
    public SortDirection? SortDirectionEnum { get; set; }

    public void ApplySort(string sortColumn, SortDirection? sortDirection)
    {
        SortColumn = sortColumn;
        SortDirectionEnum = sortDirection;
    }

    private SortDirection GetSortDirection(string key) => IsActiveSortColumn(key) ? SortDirectionEnum.Value : SortDirection.Ascending;

    private bool IsActiveSortColumn(string key) => SortColumn == key && SortDirectionEnum != null;

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

    public IEnumerable<T> Sort<T>(IEnumerable<T> list) where T : class
    {
        if (!string.IsNullOrEmpty(SortColumn) && SortDirectionEnum != null)
        {
            var prop = typeof(T).GetProperty(SortColumn);

            if (prop != null)
            {
                if (SortDirectionEnum == SortDirection.Ascending)
                {
                    list = list.OrderBy(x => GetValue(prop, x));
                }
                else
                {
                    list = list.OrderByDescending(x => GetValue(prop, x));
                }
            }
        }

        return list;
    }

    private object GetValue<T>(PropertyInfo prop, T element) where T : class
    {
        var value = prop.GetValue(element);

        // Value should be IComparable
        if (value is Regex regex)
        {
            return regex.ToString();
        }

        return value;
    }
}
