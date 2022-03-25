namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public record TypedUrl
    {
        public string Url { get; set; }
        public UrlType Type { get; set; }
    }
}
