// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Core.Redirects;

/// <summary>
/// Stops rules that keep sending a request to another url that isn't found either, such as /a -> /b/c with
/// /b -> /a/d, or ^/(.*)$ -> /en/$1. The rules alone can't tell, as they are fine when the new url exists, so
/// this counts the redirects that really happen: a new url is one hop further than the url redirecting to it.
/// Rules without a loop can't get past the length of their longest chain, however many clients follow them.
/// </summary>
public sealed class RedirectLoopGuard : IDisposable
{
    private const int MaxTrackedUrls = 10_000;
    private static readonly TimeSpan HopLifetime = TimeSpan.FromMinutes(1);

    // Note: A browser follows a redirect right away, so this only has to outlive one hop. When it's full,
    // a url is just not counted, and the browser's own limit still applies.
    private readonly MemoryCache _hops = new(new MemoryCacheOptions { SizeLimit = MaxTrackedUrls });
    private readonly int _maxHops;

    public RedirectLoopGuard(IOptions<NotFoundHandlerOptions> options)
    {
        _maxHops = options.Value.MaxRedirectHops;
    }

    /// <summary>
    /// Counts a redirect from a url to a new url, unless the url was reached after too many redirects.
    /// </summary>
    /// <returns><c>false</c> if the url shouldn't be redirected further.</returns>
    public bool TryRedirect(Uri url, string newUrl)
    {
        if (_maxHops <= 0)
        {
            return true;
        }

        var key = ToKey(url);
        var hops = _hops.TryGetValue(key, out int counted) ? counted : 0;

        if (hops >= _maxHops)
        {
            // Note: Start over, so a corrected rule works for the next request.
            _hops.Remove(key);
            return false;
        }

        if (Uri.TryCreate(url, newUrl, out var target))
        {
            _hops.Set(ToKey(target), hops + 1, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = HopLifetime });
        }

        return true;
    }

    public void Dispose()
    {
        _hops.Dispose();
    }

    /// <summary>
    /// A hash, so requests with long urls can't fill the memory. A collision only adds to a count.
    /// The scheme is left out, as a request can reach the site over another one than a proxy forwards.
    /// </summary>
    private static int ToKey(Uri url)
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(url.Authority + url.PathAndQuery);
    }
}
