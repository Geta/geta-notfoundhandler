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

    // Note: A browser follows a redirect right away, so this only has to outlive one hop. It's also how long
    // a url that was stopped stays stopped, so how long it takes for a corrected rule to work.
    private static readonly TimeSpan HopLifetime = TimeSpan.FromSeconds(30);

    // Note: When it's full, the oldest counts go, and the browser's own limit still applies to those urls.
    // Compacting a larger part at a time avoids sorting all the entries over and over under a flood of urls.
    private readonly MemoryCache _hops = new(new MemoryCacheOptions { SizeLimit = MaxTrackedUrls, CompactionPercentage = 0.25 });
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
        var hops = _hops.Get<int>(key);

        if (hops >= _maxHops)
        {
            return false;
        }

        if (!Uri.TryCreate(url, newUrl, out var target))
        {
            return true;
        }

        var targetKey = ToKey(target);

        // Note: A url redirecting to another spelling of itself (http://site/a -> https://site/a) would raise
        // its own count, which then adds up over separate requests. Only raise a count, or a request coming
        // into a loop from outside would reset it.
        if (targetKey != key && _hops.Get<int>(targetKey) <= hops)
        {
            _hops.Set(targetKey, hops + 1, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = HopLifetime });
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
    /// The host is in its ASCII form and the rest decoded, as a new url can be written differently than
    /// the request following it arrives: a browser and the server encode more than <see cref="Uri"/> does.
    /// </summary>
    private static int ToKey(Uri url)
    {
        var host = url.IsDefaultPort ? url.IdnHost : $"{url.IdnHost}:{url.Port}";

        return (host + Uri.UnescapeDataString(url.PathAndQuery)).GetHashCode();
    }
}
