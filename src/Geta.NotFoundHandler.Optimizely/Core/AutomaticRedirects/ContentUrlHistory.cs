using System;
using System.Collections.Generic;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class ContentUrlHistory
    {
        public Guid Id { get; set; }
        public string ContentKey { get; set; }
        public DateTime CreatedUtc { get; set; }
        public ICollection<TypedUrl> Urls { get; set; }
    }
}
