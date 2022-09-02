using System.Text.RegularExpressions;
using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Data;
using Microsoft.Extensions.Logging;

namespace Geta.NotFoundHandler.Providers.RegexRedirects;

public class RegexRedirectNotFoundHandler : INotFoundHandler
{
    private readonly IRegexRedirectLoader _regexRedirectLoader;
    private readonly ILogger<RegexRedirectNotFoundHandler> _logger;

    public RegexRedirectNotFoundHandler(IRegexRedirectLoader regexRedirectLoader, ILogger<RegexRedirectNotFoundHandler> logger)
    {
        _regexRedirectLoader = regexRedirectLoader;
        _logger = logger;
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
                    return new RewriteResult(newUrl, RedirectType.Temporary);
                }
            }
            catch (RegexMatchTimeoutException e)
            {
                _logger.LogWarning(e,
                                   "Regex URL match timed out. Url: {Url}; Regex: {Regex}; Timeout: {Timeout}",
                                   e.Input,
                                   e.Pattern,
                                   e.MatchTimeout);
            }
        }

        return RewriteResult.Empty;
    }
}
