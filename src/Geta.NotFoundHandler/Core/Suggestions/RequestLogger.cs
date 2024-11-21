// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Core.Suggestions
{
    public class RequestLogger : IRequestLogger
    {
        private readonly ILogger<RequestLogger> _logger;
        private readonly ISuggestionRepository _suggestionRepository;
        private readonly NotFoundHandlerOptions _configuration;
        private readonly Regex _ignorePatternRegex;

        public RequestLogger(
            IOptions<NotFoundHandlerOptions> options,
            ILogger<RequestLogger> logger,
            ISuggestionRepository suggestionRepository)
        {
            _logger = logger;
            _suggestionRepository = suggestionRepository;
            _configuration = options.Value;

            if (!string.IsNullOrWhiteSpace(_configuration.IgnoreSuggestionsUrlRegexPattern))
            {
                _ignorePatternRegex = new Regex(_configuration.IgnoreSuggestionsUrlRegexPattern, RegexOptions.Compiled);
            }
        }

        public void LogRequest(string oldUrl, string referer)
        {
            var bufferSize = _configuration.BufferSize;
            if (!LogQueue.IsEmpty && LogQueue.Count >= bufferSize)
            {
                lock (LogQueue)
                {
                    try
                    {
                        if (LogQueue.Count >= bufferSize)
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

        private void LogRequests(ConcurrentQueue<LogEvent> logEvents)
        {
            _logger.LogDebug("Logging 404 errors to database");
            var bufferSize = _configuration.BufferSize;
            var threshold = _configuration.ThreshHold;
            var start = logEvents.First().Requested;
            var end = logEvents.Last().Requested;
            var diff = (end - start).Seconds;

            if ((diff != 0 && bufferSize / diff <= threshold)
                || bufferSize == 0)
            {
                while (!logEvents.IsEmpty)
                {
                    if (logEvents.TryDequeue(out var logEvent))
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

        public bool ShouldLogRequest(string url)
        {
            if (_configuration.Logging == LoggerMode.Off)
            {
                return false;
            }

            if (_ignorePatternRegex == null)
            {
                return true;
            }

            return !_ignorePatternRegex.IsMatch(url);
        }

        private static ConcurrentQueue<LogEvent> LogQueue { get; } = new ConcurrentQueue<LogEvent>();
    }
}
