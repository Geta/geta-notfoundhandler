using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Components.Card
{
    public class CardViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(string message)
        {
            return View(new CardViewModel { Message = message });
        }
    }
}
