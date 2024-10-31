using Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Models;

namespace Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Components.SortableHeaderCell;

public class SortableHeaderCellViewModel
{
    public string DisplayName { get; set; }
    public string Key { get; set; }
    public SortDirection SortDirection { get; set; }
    public bool IsActive { get; set; }
}
