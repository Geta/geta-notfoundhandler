using System;
using EPiServer;
using EPiServer.Core;
using Geta.NotFoundHandler.Optimizely.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class ContentUrlHistoryEvents
    {
        private readonly IContentEvents _contentEvents;
        private readonly Func<ContentUrlIndexer> _contentUrlIndexerFactory;
        private readonly OptimizelyNotFoundHandlerOptions _configuration;

        public ContentUrlHistoryEvents(
            IContentEvents contentEvents,
            IOptions<OptimizelyNotFoundHandlerOptions> options,
            Func<ContentUrlIndexer> contentUrlIndexerFactory)
        {
            _contentEvents = contentEvents;
            _contentUrlIndexerFactory = contentUrlIndexerFactory;
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
        }

        private void OnPublishedContent(object sender, ContentEventArgs e)
        {
            IndexContentUrl(e);
        }

        private void IndexContentUrl(ContentEventArgs e)
        {
            var indexer = _contentUrlIndexerFactory();
            indexer.IndexContentUrl(e.ContentLink);
        }
    }
}
