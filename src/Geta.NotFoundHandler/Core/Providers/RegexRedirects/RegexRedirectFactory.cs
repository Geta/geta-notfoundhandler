using System;
using System.Text.RegularExpressions;

namespace Geta.NotFoundHandler.Core.Providers.RegexRedirects;

public class RegexRedirectFactory
{
    private const int Timeout = 100; // TODO: Read from configuration

    public virtual RegexRedirect Create(
        Guid id,
        string oldUrlRegex,
        string newUrlFormat,
        int orderNumber,
        int? timeoutCount = null)
    {
        return new RegexRedirect(id,
                                 new Regex(oldUrlRegex, RegexOptions.Compiled, TimeSpan.FromMilliseconds(Timeout)),
                                 newUrlFormat,
                                 orderNumber,
                                 timeoutCount);
    }

    public virtual RegexRedirect CreateNew(string oldUrlRegex, string newUrlFormat, int orderNumber)
    {
        return new RegexRedirect(null,
                                 new Regex(oldUrlRegex, RegexOptions.Compiled, TimeSpan.FromMilliseconds(Timeout)),
                                 newUrlFormat,
                                 orderNumber,
                                 0);
    }

    public virtual RegexRedirect CreateForDeletion(Guid id)
    {
        return new RegexRedirect(id, null, string.Empty, 0, null);
    }
}
