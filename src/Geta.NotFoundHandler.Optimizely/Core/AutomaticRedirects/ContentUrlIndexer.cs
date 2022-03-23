using System.Linq;
using EPiServer.Core;
using Geta.NotFoundHandler.Data;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class ContentUrlIndexer
    {
        private readonly ContentKeyGenerator _contentKeyGenerator;
        private readonly ContentUrlLoader _contentUrlLoader;
        private readonly IRepository<ContentUrlHistory> _contentUrlHistoryRepository;

        public ContentUrlIndexer(
            ContentKeyGenerator contentKeyGenerator,
            ContentUrlLoader contentUrlLoader,
            IRepository<ContentUrlHistory> contentUrlHistoryRepository)
        {
            _contentKeyGenerator = contentKeyGenerator;
            _contentUrlLoader = contentUrlLoader;
            _contentUrlHistoryRepository = contentUrlHistoryRepository;
        }

        public virtual void IndexContentUrl(ContentReference contentLink)
        {
            var keyResult = _contentKeyGenerator.GetContentKey(contentLink);
            if (!keyResult.HasValue) return;

            var urls = _contentUrlLoader.GetUrls(contentLink).ToList();
            var history = new ContentUrlHistory { ContentKey = keyResult.Key, Urls = urls };
            _contentUrlHistoryRepository.Save(history);
        }
    }
}
