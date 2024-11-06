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

    public IViewComponentResult Invoke(string key, string displayName)
    {
        var context = _contextAccessor.HttpContext;

        return View(new SortableHeaderCellViewModel
        {
            QueryString = context?.Request.QueryString.ToString(),
            Key = key,
            DisplayName = displayName
        });
    }
}
