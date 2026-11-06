// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Geta.NotFoundHandler.Infrastructure.Processing;

namespace Geta.NotFoundHandler.Core.Redirects;

/// <summary>
/// A collection of custom urls
/// </summary>
public class CustomRedirectCollection : IEnumerable<CustomRedirect>
{
    private const string PathSeparators = "/\\";

    private readonly IEnumerable<INotFoundHandler> _providers = new List<INotFoundHandler>();

    /// <summary>
    /// Hashtable for quick lookup of old urls
    /// </summary>
    private readonly Dictionary<string, CustomRedirect> _quickLookupTable = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Cache of URLs sorted ZA for look up of partially matched URLs
    /// </summary>
    private KeyValuePair<string, CustomRedirect>[] _redirectsZACache;

    public CustomRedirectCollection()
    {
    }

    public CustomRedirectCollection(IEnumerable<INotFoundHandler> providers)
    {
        _providers = providers;
    }

    public IEnumerator<CustomRedirect> GetEnumerator()
    {
        return _quickLookupTable.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public CustomRedirect Find(Uri urlNotFound)
    {
        // Handle absolute addresses first
        var url = urlNotFound.AbsoluteUri;
        var foundRedirect = FindInternal(url);

        // Common case
        if (foundRedirect == null)
        {
            url = urlNotFound.PathAndQuery;
            foundRedirect = FindInternal(url);
        }

        // Handle legacy databases with encoded values
        if (foundRedirect == null)
        {
            url = HttpUtility.HtmlEncode(url);
            foundRedirect = FindInternal(url);
        }

        if (foundRedirect == null)
        {
            url = urlNotFound.AbsoluteUri;
            foundRedirect = FindInProviders(url);
        }

        return foundRedirect;
    }

    public void Add(CustomRedirect customRedirect)
    {
        var oldUrl = HttpUtility.UrlDecode(customRedirect.OldUrl);
        if (_quickLookupTable.ContainsKey(oldUrl))
        {
            _quickLookupTable[oldUrl] = customRedirect;
            return;
        }

        // Add to quick look up table too
        _quickLookupTable.Add(oldUrl, customRedirect);

        // clean cache
        _redirectsZACache = null;
    }

    private CustomRedirect FindInternal(string url)
    {
        url = HttpUtility.UrlDecode(url) ?? string.Empty;
        if (_quickLookupTable.TryGetValue(url, out var redirect))
        {
            return redirect;
        }

        // working with local copy to avoid multi-threading issues
        var redirectsZA = _redirectsZACache;
        if (redirectsZA == null)
        {
            redirectsZA = _quickLookupTable.OrderByDescending(x => x.Key, StringComparer.OrdinalIgnoreCase).ToArray();
            _redirectsZACache = redirectsZA;
        }

        var path = url.AsPathSpan();
        var query = url.AsQuerySpan();

        // No exact match could be done, so we'll check if the 404 url
        // starts with one of the urls we're matching against. This
        // will be kind of a wild card match (even though we only check
        // for the start of the url
        // Example: http://www.mysite.com/news/mynews.html is not found
        // We have defined an "<old>/news</old>" entry in the config
        // file. We will get a match on the /news part of /news/myne...
        // Depending on the skip wild card append setting, we will either
        // redirect using the <new> url as is, or we'll append the 404
        // url to the <new> url.
        foreach (var redirectPair in redirectsZA)
        {
            var oldUrl = redirectPair.Key;
            if (string.IsNullOrWhiteSpace(oldUrl))
            {
                continue;
            }

            var oldPath = oldUrl.AsPathSpan();
            var oldQuery = oldUrl.AsQuerySpan();

            // See if this "old" url (the one that cannot be found) starts with one
            if (path.UrlPathMatch(oldPath) && query.StartsWith(oldQuery, StringComparison.InvariantCultureIgnoreCase))
            {
                var cr = redirectPair.Value;
                if (cr.State == (int)RedirectState.Ignored)
                {
                    return null;
                }

                if (cr.WildCardSkipAppend)
                {
                    // Remove path from original url but keep query string.
                    return CreateSkipAppendRedirect(query, cr);
                }

                if (UrlIsOldUrlsSubSegment(path, oldUrl))
                {
                    return CreateSubSegmentRedirect(path, query, cr, oldPath);
                }
            }
        }

        return null;
    }

    private static CustomRedirect CreateSubSegmentRedirect(
        ReadOnlySpan<char> path,
        ReadOnlySpan<char> query,
        CustomRedirect cr,
        ReadOnlySpan<char> oldPath)
    {
        var redirCopy = new CustomRedirect(cr);

        var newUrl = redirCopy.NewUrl;
        var newPath = newUrl.AsPathSpan();
        var newQuery = newUrl.AsQuerySpan();

        var oldSegment = oldPath.TrimEnd('/');
        var oldName = oldSegment.TrimStart('/');
        // Note: Strip every leading separator. Browsers read "\" as "/", so one left behind
        // after a new path of "/" would form a protocol-relative url pointing at another host.
        var appendSegment = path[oldSegment.Length..].TrimStart(PathSeparators);

        // Note: Guard against infinite buildup of redirects
        while (oldName.Length > 0 && appendSegment.UrlPathMatch(oldName))
        {
            appendSegment = appendSegment[oldName.Length..].TrimStart(PathSeparators);
        }

        var requestHasTrailingSlash = path.EndsWith("/");

        var size = appendSegment.Length + query.Length + newQuery.Length + 1;
        var builder = new StringBuilder(size);

        BuildPath(newPath, appendSegment, requestHasTrailingSlash, builder);

        var hasQuery = query.Length > 0 || newQuery.Length > 0;
        var hasEmptyPath = builder.Length == 0;

        if (hasEmptyPath && hasQuery)
        {
            builder.Append('/');
        }

        BuildQuery(query, newQuery, builder);

        redirCopy.NewUrl = builder.ToString();

        return redirCopy;
    }

    private static CustomRedirect CreateSkipAppendRedirect(ReadOnlySpan<char> query, CustomRedirect cr)
    {
        var redirCopy = new CustomRedirect(cr);

        var newUrl = redirCopy.NewUrl;
        var newPath = newUrl.AsPathSpan();
        var newQuery = newUrl.AsQuerySpan();

        var size = query.Length + newQuery.Length + 1;
        var builder = new StringBuilder(size);

        builder.Append(newPath);

        var hasQuery = query.Length > 0 || newQuery.Length > 0;

        if (hasQuery && !newPath.EndsWith("/"))
        {
            builder.Append('/');
        }

        BuildQuery(query, newQuery, builder);

        redirCopy.NewUrl = builder.ToString();

        return redirCopy;
    }

    private static void BuildPath(
        ReadOnlySpan<char> newPath,
        ReadOnlySpan<char> appendSegment,
        bool requestHasTrailingSlash,
        StringBuilder builder)
    {
        var hasAppendSegment = appendSegment.Length > 0;

        if (!hasAppendSegment && !requestHasTrailingSlash)
        {
            newPath = newPath.RemoveTrailingSlash();
        }

        builder.Append(newPath);

        if (hasAppendSegment && !newPath.EndsWith("/"))
        {
            builder.Append('/');
        }

        // Note: appendSegment is a suffix of the request path, so it already carries any trailing slash.
        builder.Append(appendSegment);
    }

    private static void BuildQuery(ReadOnlySpan<char> baseQuery, ReadOnlySpan<char> newQuery, StringBuilder builder)
    {
        var hasIncomingQuery = baseQuery.Length > 0;
        var hasOutgoingQuery = newQuery.Length > 0;

        if (hasIncomingQuery)
        {
            builder.Append(baseQuery);
        }

        if (hasOutgoingQuery && !baseQuery.StartsWith(newQuery))
        {
            if (hasIncomingQuery)
            {
                builder.Append('&');
                builder.Append(newQuery[1..]);
            }
            else
            {
                builder.Append(newQuery);
            }
        }
    }

    private static bool UrlIsOldUrlsSubSegment(ReadOnlySpan<char> url, string oldUrl)
    {
        ReadOnlySpan<char> RemoveQueryString(ReadOnlySpan<char> u)
        {
            var i = u.IndexOf("?", StringComparison.Ordinal);
            return i < 0 ? u : u[..i];
        }

        var normalizedUrlWithoutQuery = RemoveQueryString(url).TrimEnd('/');
        var normalizedOldUrl = RemoveQueryString(oldUrl).TrimEnd('/');
        var isSameUrl = normalizedUrlWithoutQuery.Equals(normalizedOldUrl, StringComparison.OrdinalIgnoreCase);
        var isPartOfOldUrl = normalizedUrlWithoutQuery[..normalizedOldUrl.Length].StartsWith("/");
        return isSameUrl || isPartOfOldUrl;
    }

    private CustomRedirect FindInProviders(string oldUrl)
    {
        return _providers
            .Select(provider => provider.RewriteUrl(oldUrl))
            .Where(rewriteResult => !rewriteResult.IsEmpty)
            .Select(rewriteResult => new CustomRedirect(oldUrl, rewriteResult.NewUrl, false, rewriteResult.RedirectType))
            .FirstOrDefault();
    }
}
