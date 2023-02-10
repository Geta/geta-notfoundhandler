// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Geta.NotFoundHandler.Infrastructure.Processing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Core.Suggestions
{
    public class RequestLogger : IRequestLogger
    {
        private readonly ILogger<RequestLogger> _logger;
        private readonly ISuggestionRepository _suggestionRepository;
        private readonly IRedirectsService _redirectsService;
        private readonly NotFoundHandlerOptions _configuration;

        public RequestLogger(
            IOptions<NotFoundHandlerOptions> options,
            ILogger<RequestLogger> logger,
            ISuggestionRepository suggestionRepository,
            IRedirectsService redirectsService)
        {
            _logger = logger;
            _suggestionRepository = suggestionRepository;
            _redirectsService = redirectsService;
            _configuration = options.Value;
        }

        public void LogRequest(string oldUrl, string referer)
        {
            if (AllowLogOfRequests())
            {
                lock (LogQueue)
                {
                    try
                    {
                        if (AllowLogOfRequests())
                        {
                            LogRequests(LogQueue);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while trying to log 404 errors");
                    }
                }
            }

            LogQueue.Enqueue(new LogEvent(oldUrl, DateTime.UtcNow, referer));
        }

        private bool AllowLogOfRequests()
        {
            var bufferSize = _configuration.BufferSize;
            var threshold = _configuration.ThreshHold;
            return !LogQueue.IsEmpty && ((threshold > 0 && (DateTime.UtcNow - LogQueue.Last().Requested).TotalSeconds >= threshold) || LogQueue.Count >= bufferSize);
        }

        private void LogRequests(ConcurrentQueue<LogEvent> logEvents)
        {
            _logger.LogDebug("Logging 404 errors to database");
            var bufferSize = _configuration.BufferSize;
            var threshold = _configuration.ThreshHold;
            var start = logEvents.First().Requested;
            var end = logEvents.Last().Requested;
            var diff = (int)Math.Floor((end - start).TotalSeconds);

            if ((diff != 0 && bufferSize / diff <= threshold)
                || bufferSize == 0)
            {
                var ignored = _redirectsService.GetIgnored().Select(x => x.OldUrl);
                while (!logEvents.IsEmpty)
                {
                    if (logEvents.TryDequeue(out var logEvent) && !IgnoreSuggestion(logEvent, ignored))
                    {
                        _suggestionRepository.Save(logEvent.OldUrl, logEvent.Referer, logEvent.Requested);
                    }
                }

                _logger.LogDebug("{BufferSize} 404 request(s) has been stored to the database", bufferSize);
            }
            else
            {
                _logger.LogWarning(
                    "404 requests have been made too frequents (exceeded the threshold). Requests not logged to database");
            }
        }

        private static bool IgnoreSuggestion(LogEvent logEvent, IEnumerable<string> ignored)
        {
            if (logEvent.OldUrl.StartsWith("/EPiServer/", StringComparison.InvariantCultureIgnoreCase))
                return true;

            if (ignored.Any(x => logEvent.OldUrl.AsPathSpan().UrlPathMatch(x.AsPathSpan()) && logEvent.OldUrl.AsPathSpan().StartsWith(x.AsQuerySpan(), StringComparison.InvariantCultureIgnoreCase)))
                return true;

            return false;
        }

        private static ConcurrentQueue<LogEvent> LogQueue { get; } = new ConcurrentQueue<LogEvent>();
    }
}
