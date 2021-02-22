using Microsoft.AspNetCore.Mvc;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Components.Card
{
    public class CardViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string message, CardType cardType)
        {
            return View(new CardViewModel { Message = message, CardType = cardType });
        }
    }
}
