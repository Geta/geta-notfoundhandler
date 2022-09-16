// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Threading;
using System.Threading.Tasks;
using EPiServer.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class MovedContentRegistratorBackgroundService : BackgroundService
    {
        private readonly ChannelMovedContentRegistratorQueue _registratorQueue;
        private readonly Func<ContentKeyGenerator> _contentKeyGeneratorFactory;
        private readonly IAutomaticRedirectsService _automaticRedirectsService;
        private readonly IContentUrlHistoryLoader _contentUrlHistoryLoader;
        private readonly Func<ContentUrlIndexer> _contentUrlIndexerFactory;
        private readonly ILogger<MovedContentRegistratorBackgroundService> _logger;

        public MovedContentRegistratorBackgroundService(
            ChannelMovedContentRegistratorQueue registratorQueue,
            Func<ContentKeyGenerator> contentKeyGeneratorFactory,
            IAutomaticRedirectsService automaticRedirectsService,
            IContentUrlHistoryLoader contentUrlHistoryLoader,
            Func<ContentUrlIndexer> contentUrlIndexerFactory,
            ILogger<MovedContentRegistratorBackgroundService> logger)
        {
            _registratorQueue = registratorQueue;
            _contentKeyGeneratorFactory = contentKeyGeneratorFactory;
            _automaticRedirectsService = automaticRedirectsService;
            _contentUrlHistoryLoader = contentUrlHistoryLoader;
            _contentUrlIndexerFactory = contentUrlIndexerFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var contentLink in _registratorQueue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    IndexContentUrls(contentLink);
                    CreateRedirects(contentLink);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed registering redirects for content: {ContentLink}", contentLink);
                }
            }
        }

        private void CreateRedirects(ContentReference contentLink)
        {
            var contentKeyGenerator = _contentKeyGeneratorFactory();
            var keyResult = contentKeyGenerator.GetContentKey(contentLink);
            if (!keyResult.HasValue) return;

            var contentKey = keyResult.Key;
            var histories = _contentUrlHistoryLoader.GetMoved(contentKey);
            _automaticRedirectsService.CreateRedirects(histories);
        }

        private void IndexContentUrls(ContentReference contentLink)
        {
            var contentUrlIndexer = _contentUrlIndexerFactory();
            contentUrlIndexer.IndexContentUrls(contentLink);
        }
    }
}
