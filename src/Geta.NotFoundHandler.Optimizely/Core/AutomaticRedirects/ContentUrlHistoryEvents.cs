using EPiServer;
using EPiServer.Core;
using Geta.NotFoundHandler.Optimizely.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class ContentUrlHistoryEvents
    {
        private readonly IContentEvents _contentEvents;
        private readonly ContentUrlIndexer _contentUrlIndexer;
        private readonly IMovedContentRedirectsRegistrator _movedContentRedirectsRegistrator;
        private readonly OptimizelyNotFoundHandlerOptions _configuration;

        public ContentUrlHistoryEvents(
            IContentEvents contentEvents,
            IOptions<OptimizelyNotFoundHandlerOptions> options,
            ContentUrlIndexer contentUrlIndexer,
            IMovedContentRedirectsRegistrator movedContentRedirectsRegistrator)
        {
            _contentEvents = contentEvents;
            _contentUrlIndexer = contentUrlIndexer;
            _movedContentRedirectsRegistrator = movedContentRedirectsRegistrator;
            _configuration = options.Value;
        }

        public void Initialize()
        {
            if (_configuration.AutomaticRedirectsEnabled)
            {
                _contentEvents.MovedContent += OnMovedContent;
                _contentEvents.PublishedContent += OnPublishedContent;
            }
        }

        private void OnMovedContent(object sender, ContentEventArgs e)
        {
            IndexContentUrl(e);
            _movedContentRedirectsRegistrator.Register(e.ContentLink);
        }

        private void OnPublishedContent(object sender, ContentEventArgs e)
        {
            IndexContentUrl(e);
        }

        private void IndexContentUrl(ContentEventArgs e)
        {
            _contentUrlIndexer.IndexContentUrl(e.ContentLink);
        }
    }
}
