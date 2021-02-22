namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Components.Card
{
    public static class CardTypeExtensions
    {
        public static string GetCssClass(this CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Success:
                    return "text-white bg-success";
                default:
                    return string.Empty;
            }
        }
    }
}
