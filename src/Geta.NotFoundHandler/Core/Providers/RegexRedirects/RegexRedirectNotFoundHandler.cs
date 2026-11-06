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
                    var newUrl = KeepOnSite(match.Result(redirect.NewUrlFormat), redirect.NewUrlFormat);
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

    /// <summary>
    /// Text captured from the request must not turn a relative format such as "/$1" into a
    /// protocol-relative url pointing at another host. A format that names a host itself is left alone.
    /// </summary>
    private static string KeepOnSite(string newUrl, string format)
    {
        return IsProtocolRelative(newUrl) && !IsProtocolRelative(format)
            ? "/" + newUrl.TrimStart('/', '\\')
            : newUrl;
    }

    private static bool IsProtocolRelative(string url)
    {
        return url.Length > 1 && url[0] is '/' or '\\' && url[1] is '/' or '\\';
    }
}
