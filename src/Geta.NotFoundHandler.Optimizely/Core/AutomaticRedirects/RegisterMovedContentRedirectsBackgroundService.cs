using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class RegisterMovedContentRedirectsBackgroundService : BackgroundService
    {
        private readonly ChannelMovedContentRedirectsRegistrator _redirectsRegistrator;
        private readonly ContentKeyGenerator _contentKeyGenerator;
        private readonly IAutomaticRedirectsService _automaticRedirectsService;
        private readonly IContentUrlHistoryLoader _contentUrlHistoryLoader;
        private readonly ILogger<RegisterMovedContentRedirectsBackgroundService> _logger;

        public RegisterMovedContentRedirectsBackgroundService(
            ChannelMovedContentRedirectsRegistrator redirectsRegistrator,
            ContentKeyGenerator contentKeyGenerator,
            IAutomaticRedirectsService automaticRedirectsService,
            IContentUrlHistoryLoader contentUrlHistoryLoader,
            ILogger<RegisterMovedContentRedirectsBackgroundService> logger)
        {
            _redirectsRegistrator = redirectsRegistrator;
            _contentKeyGenerator = contentKeyGenerator;
            _automaticRedirectsService = automaticRedirectsService;
            _contentUrlHistoryLoader = contentUrlHistoryLoader;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (await _redirectsRegistrator.WaitToReadAsync(cancellationToken))
            while (_redirectsRegistrator.TryRead(out var contentLink))
            {
                try
                {
                    var keyResult = _contentKeyGenerator.GetContentKey(contentLink);
                    if (!keyResult.HasValue)
                    {
                        continue;
                    }

                    var contentKey = keyResult.Key;
                    var histories = _contentUrlHistoryLoader.GetMoved(contentKey);
                    _automaticRedirectsService.CreateRedirects(histories);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed registering redirects for content: {ContentLink}", contentLink);
                }
            }
        }
    }
}
