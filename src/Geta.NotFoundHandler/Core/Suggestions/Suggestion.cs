using System;

namespace Geta.NotFoundHandler.Core.Suggestions
{
    public class Suggestion
    {
        public int Id { get; set; }
        public string OldUrl { get; set; }
        public DateTime? Requested { get; set; }
        public string Referer { get; set; }
    }
}
