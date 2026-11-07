// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Text.RegularExpressions;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Core.Providers.RegexRedirects;

public class RegexRedirectNotFoundHandler : INotFoundHandler
{
    // Note: Browsers also read "https:evil.com" as a host, which Uri doesn't parse as absolute.
    private static readonly Regex StartsWithScheme = new("^[a-z][a-z0-9+.-]*:", RegexOptions.IgnoreCase);

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

        // Note: A rule like "^/en/blog/(.*)" can't match a whole url, so each rule also gets the path and query.
        var pathAndQuery = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.PathAndQuery : url;

        foreach (var redirect in regexRedirects)
        {
            try
            {
                var match = redirect.OldUrlRegex.Match(url);
                if (!match.Success)
                {
                    match = redirect.OldUrlRegex.Match(pathAndQuery);
                }

                if (match.Success)
                {
                    var newUrl = KeepOnSite(match.Result(redirect.NewUrlFormat), redirect.NewUrlFormat, url);
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
    /// Text captured from the request must not turn a format such as "/$1" or "$1" into a url
    /// pointing at another host. A format that names a host itself is left alone, and so is a
    /// captured scheme and host that are the ones of the request ("$1/new" capturing "https://this.site").
    /// </summary>
    private static string KeepOnSite(string newUrl, string format, string requestUrl)
    {
        // Note: "://" also covers a format that captures the scheme and names the host ("$1://new.site/$2").
        var formatNamesHost = NamesHost(format) || format.Contains("://");

        return NamesHost(newUrl) && !formatNamesHost && !IsOnSameSite(newUrl, requestUrl)
            ? "/" + newUrl.TrimStart('/', '\\')
            : newUrl;
    }

    private static bool NamesHost(string url)
    {
        var isProtocolRelative = url.Length > 1 && url[0] is '/' or '\\' && url[1] is '/' or '\\';

        return isProtocolRelative || StartsWithScheme.IsMatch(url);
    }

    private static bool IsOnSameSite(string url, string requestUrl)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
               && Uri.TryCreate(requestUrl, UriKind.Absolute, out var requestUri)
               && uri.Authority.Equals(requestUri.Authority, StringComparison.OrdinalIgnoreCase);
    }
}
