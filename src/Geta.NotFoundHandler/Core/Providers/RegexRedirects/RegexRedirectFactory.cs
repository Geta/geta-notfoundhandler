using System;
using System.Text.RegularExpressions;

namespace Geta.NotFoundHandler.Core.Providers.RegexRedirects;

public class RegexRedirectFactory
{
    private const int Timeout = 100; // TODO: Read from configuration

    public virtual RegexRedirect Create(Guid id, string oldUrlRegex, string newUrlFormat, int orderNumber, int timeoutCount)
    {
        return new RegexRedirect(id,
                                 new Regex(oldUrlRegex, RegexOptions.Compiled, TimeSpan.FromMilliseconds(Timeout)),
                                 newUrlFormat,
                                 orderNumber,
                                 timeoutCount);
    }
}
