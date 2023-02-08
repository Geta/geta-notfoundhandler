using Geta.NotFoundHandler.Data;
using Microsoft.AspNetCore.Mvc;

namespace Geta.NotFoundHandler.Admin.Areas.GetaNotFoundHandlerAdmin.Pages.Components.SortableHeader
{
    public class SortableHeaderViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string displayName, string internalName, QueryParams @params, string additionalClass = null)
        {
            return View(new SortableHeaderViewModel(displayName, internalName, @params, additionalClass));
        }
    }
}
