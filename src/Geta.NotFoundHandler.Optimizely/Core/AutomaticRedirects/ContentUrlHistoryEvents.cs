// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

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
        private readonly IMovedContentRegistratorQueue _movedContentRegistratorQueue;
        private readonly OptimizelyNotFoundHandlerOptions _configuration;

        public ContentUrlHistoryEvents(
            IContentEvents contentEvents,
            IOptions<OptimizelyNotFoundHandlerOptions> options,
            ContentUrlIndexer contentUrlIndexer,
            IMovedContentRegistratorQueue movedContentRegistratorQueue)
        {
            _contentEvents = contentEvents;
            _contentUrlIndexer = contentUrlIndexer;
            _movedContentRegistratorQueue = movedContentRegistratorQueue;
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
            _movedContentRegistratorQueue.Enqueue(e.ContentLink);
        }

        private void OnPublishedContent(object sender, ContentEventArgs e)
        {
            _contentUrlIndexer.IndexContentUrls(e.ContentLink);
        }
    }
}
