using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Models;

namespace Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<T> Sort<T>(this IEnumerable<T> list, string propertyName, SortDirection? sortDirection)
    {
        if (!string.IsNullOrEmpty(propertyName))
        {
            var prop = typeof(T).GetProperty(propertyName);

            if (prop != null && sortDirection != null)
            {
                if (sortDirection == SortDirection.Ascending)
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
    
    private static object GetValue<T>(PropertyInfo prop, T element)
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
