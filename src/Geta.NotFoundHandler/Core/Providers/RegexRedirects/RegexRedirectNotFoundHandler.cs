// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Core.Providers.RegexRedirects;

public class RegexRedirectNotFoundHandler : INotFoundHandler
{
    private readonly IRegexRedirectLoader _regexRedirectLoader;
    private readonly ILogger<RegexRedirectNotFoundHandler> _logger;
    private readonly NotFoundHandlerOptions _options;

    public RegexRedirectNotFoundHandler(
        IRegexRedirectLoader regexRedirectLoader, 
        ILogger<RegexRedirectNotFoundHandler> logger, 
        IOptions<NotFoundHandlerOptions> options)
    {
        _regexRedirectLoader = regexRedirectLoader;
        _logger = logger;
        _options = options.Value;
    }

    public RewriteResult RewriteUrl(string url)
    {
        var regexRedirects = _regexRedirectLoader.GetAll();

        foreach (var redirect in regexRedirects)
        {
            try
            {
                var match = redirect.OldUrlRegex.Match(url);
                if (match.Success)
                {
                    var newUrl = match.Result(redirect.NewUrlFormat);
                    return new RewriteResult(newUrl, _options.DefaultRedirectType);
                }
            }
            catch (RegexMatchTimeoutException e)
            {
                _logger.LogWarning(e,
                                   "Regex URL match timed out. Url: {Url}; Regex: {Regex}; Timeout: {Timeout}",
                                   e.Input,
                                   e.Pattern,
                                   e.MatchTimeout);
                
                // TODO: Record timeout failure
            }
        }

        return RewriteResult.Empty;
    }
}
