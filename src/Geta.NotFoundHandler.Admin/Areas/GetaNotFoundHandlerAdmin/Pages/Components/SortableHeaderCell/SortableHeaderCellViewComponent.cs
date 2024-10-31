using Microsoft.AspNetCore.Mvc;

namespace Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Components.SortableHeaderCell;

public class SortableHeaderCellViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(SortableHeaderCellViewModel sortableHeaderCellViewModel)
    {
        return View(sortableHeaderCellViewModel);
    }
}
