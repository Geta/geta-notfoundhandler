using Microsoft.AspNetCore.Mvc;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Components.CheckboxReadonly
{
    public class CheckboxReadonlyViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(bool isChecked)
        {
            return View(new CheckboxReadonlyViewModel { IsChecked = isChecked });
        }
    }
}
