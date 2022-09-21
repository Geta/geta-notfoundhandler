using System;
using System.Text.RegularExpressions;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Core.Providers.RegexRedirects;

public class RegexRedirectFactory
{
    private readonly NotFoundHandlerOptions _configuration;

    public RegexRedirectFactory(IOptions<NotFoundHandlerOptions> options)
    {
        _configuration = options.Value;
    }

    public virtual RegexRedirect Create(
        Guid id,
        string oldUrlRegex,
        string newUrlFormat,
        int orderNumber,
        int? timeoutCount = null,
        DateTime? createdAt = null,
        DateTime? modifiedAt = null)
    {
        return new RegexRedirect(id,
                                 new Regex(oldUrlRegex, RegexOptions.Compiled, _configuration.RegexTimeout),
                                 newUrlFormat,
                                 orderNumber,
                                 timeoutCount,
                                 createdAt,
                                 modifiedAt);
    }

    public virtual RegexRedirect CreateNew(string oldUrlRegex, string newUrlFormat, int orderNumber)
    {
        return new RegexRedirect(null,
                                 new Regex(oldUrlRegex, RegexOptions.Compiled, _configuration.RegexTimeout),
                                 newUrlFormat,
                                 orderNumber,
                                 0,
                                 null,
                                 null);
    }

    public virtual RegexRedirect CreateForDeletion(Guid id)
    {
        return new RegexRedirect(id, null, string.Empty, 0, null, null, null);
    }
}
