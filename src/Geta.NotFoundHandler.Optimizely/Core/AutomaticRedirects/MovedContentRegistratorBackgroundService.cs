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
        private readonly ContentKeyGenerator _contentKeyGenerator;
        private readonly IAutomaticRedirectsService _automaticRedirectsService;
        private readonly IContentUrlHistoryLoader _contentUrlHistoryLoader;
        private readonly ContentUrlIndexer _contentUrlIndexer;
        private readonly ILogger<MovedContentRegistratorBackgroundService> _logger;

        public MovedContentRegistratorBackgroundService(
            ChannelMovedContentRegistratorQueue registratorQueue,
            ContentKeyGenerator contentKeyGenerator,
            IAutomaticRedirectsService automaticRedirectsService,
            IContentUrlHistoryLoader contentUrlHistoryLoader,
            ContentUrlIndexer contentUrlIndexer,
            ILogger<MovedContentRegistratorBackgroundService> logger)
        {
            _registratorQueue = registratorQueue;
            _contentKeyGenerator = contentKeyGenerator;
            _automaticRedirectsService = automaticRedirectsService;
            _contentUrlHistoryLoader = contentUrlHistoryLoader;
            _contentUrlIndexer = contentUrlIndexer;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await foreach (var contentLink in _registratorQueue.ReadAllAsync(cancellationToken))
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
            var keyResult = _contentKeyGenerator.GetContentKey(contentLink);
            if (!keyResult.HasValue)
            {
                return;
            }

            var contentKey = keyResult.Key;
            var histories = _contentUrlHistoryLoader.GetMoved(contentKey);
            _automaticRedirectsService.CreateRedirects(histories);
        }

        private void IndexContentUrls(ContentReference contentLink)
        {
            _contentUrlIndexer.IndexContentUrls(contentLink);
        }
    }
}
