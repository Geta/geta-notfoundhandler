using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Components.SortableHeaderCell;

public class SortableHeaderCellViewComponent : ViewComponent
{
    private readonly IHttpContextAccessor _contextAccessor;

    public SortableHeaderCellViewComponent(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public IViewComponentResult Invoke(SortableHeaderCellViewModel sortableHeaderCellViewModel)
    {
        var context = _contextAccessor.HttpContext;

        sortableHeaderCellViewModel.QueryString = context?.Request.QueryString.ToString();

        return View(sortableHeaderCellViewModel);
    }
}
