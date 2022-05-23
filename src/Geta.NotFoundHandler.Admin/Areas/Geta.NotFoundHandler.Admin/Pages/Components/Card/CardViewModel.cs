namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Components.Card
{
    public class CardViewModel
    {
        public string Message { get; set; }
        public bool HasMessage => !string.IsNullOrEmpty(Message);
        public CardType CardType { get; set; } = CardType.Default;
    }
}
