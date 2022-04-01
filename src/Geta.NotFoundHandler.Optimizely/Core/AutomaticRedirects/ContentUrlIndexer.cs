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
        private readonly IContentUrlHistoryLoader _contentUrlHistoryLoader;

        public ContentUrlIndexer(
            ContentKeyGenerator contentKeyGenerator,
            ContentUrlLoader contentUrlLoader,
            IRepository<ContentUrlHistory> contentUrlHistoryRepository,
            IContentUrlHistoryLoader contentUrlHistoryLoader)
        {
            _contentKeyGenerator = contentKeyGenerator;
            _contentUrlLoader = contentUrlLoader;
            _contentUrlHistoryRepository = contentUrlHistoryRepository;
            _contentUrlHistoryLoader = contentUrlHistoryLoader;
        }

        public virtual void IndexContentUrls(ContentReference contentLink)
        {
            var keyResult = _contentKeyGenerator.GetContentKey(contentLink);
            if (!keyResult.HasValue) return;

            var urls = _contentUrlLoader.GetUrls(contentLink).ToList();
            var history = new ContentUrlHistory { ContentKey = keyResult.Key, Urls = urls };

            if (!_contentUrlHistoryLoader.IsRegistered(history))
            {
                _contentUrlHistoryRepository.Save(history);
            }
        }
    }
}
