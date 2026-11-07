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

    /// <summary>
    /// Cache of the ignored urls that are patterns
    /// </summary>
    private KeyValuePair<string, CustomRedirect>[] _ignorePatternsCache;

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
        var foundRedirect = FindInternal(url, urlNotFound.Authority);

        // Common case
        if (foundRedirect == null)
        {
            url = urlNotFound.PathAndQuery;
            foundRedirect = FindInternal(url, urlNotFound.Authority);
        }

        // Handle legacy databases with encoded values
        if (foundRedirect == null)
        {
            url = HttpUtility.HtmlEncode(url);
            foundRedirect = FindInternal(url, urlNotFound.Authority);
        }

        if (foundRedirect == null)
        {
            url = urlNotFound.AbsoluteUri;
            foundRedirect = FindInProviders(url);
        }

        // Note: Last, so a pattern only stops suggestions, and never a redirect that applies to the url.
        return foundRedirect ?? FindIgnorePattern(urlNotFound);
    }

    public void Add(CustomRedirect customRedirect)
    {
        _ignorePatternsCache = null;

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

    private CustomRedirect FindInternal(string url, string requestAuthority)
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

                // Note: Leave it to a shorter old url, which may still apply to the request.
                if (cr.ExactMatch && !IsExactMatch(path, query, oldPath, oldQuery))
                {
                    continue;
                }

                if (cr.State == (int)RedirectState.Ignored)
                {
                    return null;
                }

                if (cr.WildCardSkipAppend)
                {
                    // Remove path from original url but keep query string.
                    return HasQueryAlreadyBeenAdded(path, query, cr.NewUrl, requestAuthority)
                        ? null
                        : CreateSkipAppendRedirect(query, cr);
                }

                if (UrlIsOldUrlsSubSegment(path, oldUrl))
                {
                    // Note: Don't try shorter old urls, as they would redirect the request somewhere unrelated.
                    return HasAlreadyBeenRedirected(path, query, oldPath, cr.NewUrl, requestAuthority)
                        ? null
                        : CreateSubSegmentRedirect(path, query, cr, oldPath);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// A new url nested under the old one (/a -> /a/b), or on the same path with another query string
    /// (/a -> /a?x=1), matches the old url again after redirecting. Appending again would grow the url
    /// on every hop (/a/b/b/b... or /a?x=1&amp;x=1...).
    /// </summary>
    private static bool HasAlreadyBeenRedirected(
        ReadOnlySpan<char> path,
        ReadOnlySpan<char> query,
        ReadOnlySpan<char> oldPath,
        string newUrl,
        string requestAuthority)
    {
        var localNewUrl = ToLocalNewUrl(newUrl, requestAuthority);
        var newPath = localNewUrl.AsPathSpan();
        var localOldPath = ToLocalUrl(oldPath.ToString(), requestAuthority).AsSpan();
        var localPath = ToLocalUrl(path.ToString(), requestAuthority).AsSpan();

        if (!newPath.UrlPathMatch(localOldPath) || !localPath.UrlPathMatch(newPath))
        {
            return false;
        }

        var isNested = newPath.TrimEnd('/').Length > localOldPath.TrimEnd('/').Length;

        return isNested || QueryContains(query, localNewUrl.AsQuerySpan());
    }

    /// <summary>
    /// A redirect skipping the wildcard append sends everything under the old url to the same new url,
    /// so only a request for that new url itself, with the query string already added, has been redirected.
    /// </summary>
    private static bool HasQueryAlreadyBeenAdded(
        ReadOnlySpan<char> path,
        ReadOnlySpan<char> query,
        string newUrl,
        string requestAuthority)
    {
        var localNewUrl = ToLocalNewUrl(newUrl, requestAuthority);
        var localPath = ToLocalUrl(path.ToString(), requestAuthority).AsSpan();

        return IsSamePath(localPath, localNewUrl.AsPathSpan()) && QueryContains(query, localNewUrl.AsQuerySpan());
    }

    /// <summary>
    /// The request is already known to start with the old url. Further query parameters are fine, but the
    /// query string of the old url has to end on a whole parameter, so /a?x=1 doesn't match /a?x=12.
    /// </summary>
    private static bool IsExactMatch(
        ReadOnlySpan<char> path,
        ReadOnlySpan<char> query,
        ReadOnlySpan<char> oldPath,
        ReadOnlySpan<char> oldQuery)
    {
        var endsOnWholeParameter = oldQuery.Length <= 1 || query.Length == oldQuery.Length || query[oldQuery.Length] == '&';

        return endsOnWholeParameter && IsSamePath(path, oldPath);
    }

    private static bool IsSamePath(ReadOnlySpan<char> path, ReadOnlySpan<char> otherPath)
    {
        return path.TrimEnd('/').Equals(otherPath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
    }

    private static bool QueryContains(ReadOnlySpan<char> query, ReadOnlySpan<char> other)
    {
        if (query.IsEmpty || other.IsEmpty)
        {
            return false;
        }

        // Note: Find also looks up the url html encoded for legacy values, which encodes the ampersands.
        var decodedQuery = HttpUtility.HtmlDecode(query[1..].ToString());

        // Note: The ampersands make it match whole parameters, so x=1 isn't found in x=12.
        return $"&{decodedQuery}&".Contains(string.Concat("&", other[1..], "&"), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A browser never sends the fragment back, so it can't be part of what the request is compared to.
    /// </summary>
    private static string ToLocalNewUrl(string newUrl, string requestAuthority)
    {
        var decodedUrl = HttpUtility.HtmlDecode(newUrl);
        var fragmentIndex = decodedUrl.IndexOf('#');

        return ToLocalUrl(fragmentIndex < 0 ? decodedUrl : decodedUrl[..fragmentIndex], requestAuthority);
    }

    /// <summary>
    /// Gives a url in the form the request is compared in. The request is url decoded before matching and
    /// html encoded in the lookup for legacy values, and it's absolute in the lookup for absolute old urls.
    /// </summary>
    private static string ToLocalUrl(string url, string requestAuthority)
    {
        return HttpUtility.UrlDecode(RemoveRequestAuthority(HttpUtility.HtmlDecode(url), requestAuthority));
    }

    /// <summary>
    /// Makes a url on the site of the request relative, so it compares to a relative url.
    /// A url on another site stays as it is, because its path says nothing about this one.
    /// </summary>
    private static string RemoveRequestAuthority(string url, string requestAuthority)
    {
        var isProtocolRelative = url.StartsWith("//", StringComparison.Ordinal);
        if (url.StartsWith('/') && !isProtocolRelative)
        {
            return url;
        }

        var isOnRequestSite = Uri.TryCreate(isProtocolRelative ? Uri.UriSchemeHttp + ":" + url : url, UriKind.Absolute, out var uri)
                              && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                              && uri.Authority.Equals(requestAuthority, StringComparison.OrdinalIgnoreCase);

        return isOnRequestSite ? uri.PathAndQuery : url;
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

        // Note: Collapse an old url repeated after itself (/a/a/file)
        while (oldName.Length > 0 && appendSegment.UrlPathMatch(oldName))
        {
            appendSegment = appendSegment[oldName.Length..].TrimStart(PathSeparators);
        }

        var requestHasTrailingSlash = path.EndsWith("/");

        var size = appendSegment.Length + query.Length + newQuery.Length + 1;
        var builder = new StringBuilder(size);

        BuildPath(newPath, appendSegment, requestHasTrailingSlash, builder);

        // Note: An empty url would make the browser request the same url again.
        if (builder.Length == 0)
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

    private CustomRedirect FindIgnorePattern(Uri urlNotFound)
    {
        // working with local copy to avoid multi-threading issues
        var patterns = _ignorePatternsCache;
        if (patterns == null)
        {
            // Note: The key is url decoded. A row with an encoded %2A is a literal url, so the saved value has to have the *.
            patterns = _quickLookupTable
                .Where(x => x.Value.State == (int)RedirectState.Ignored
                            && IgnorePattern.IsPattern(x.Value.OldUrl)
                            && IgnorePattern.IsPattern(x.Key))
                .ToArray();
            _ignorePatternsCache = patterns;
        }

        if (patterns.Length == 0)
        {
            return null;
        }

        var absoluteUrl = HttpUtility.UrlDecode(urlNotFound.AbsoluteUri);
        var localUrl = HttpUtility.UrlDecode(urlNotFound.PathAndQuery);

        foreach (var pattern in patterns)
        {
            if (IgnorePattern.IsMatch(pattern.Key, IgnorePattern.IsAbsolute(pattern.Key) ? absoluteUrl : localUrl))
            {
                return pattern.Value;
            }
        }

        return null;
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
