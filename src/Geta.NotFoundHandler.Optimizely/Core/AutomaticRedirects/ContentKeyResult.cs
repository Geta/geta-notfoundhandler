namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class ContentKeyResult
    {
        public static ContentKeyResult Empty = new();

        public string Key { get; }
        public bool HasValue { get; }

        private ContentKeyResult() { }

        public ContentKeyResult(string key)
        {
            Key = key;
            HasValue = true;
        }
    }
}
