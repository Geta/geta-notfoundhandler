// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.IO.Enumeration;

namespace Geta.NotFoundHandler.Core.Redirects;

/// <summary>
/// An ignored url with a * in it, such as /api/*, which ignores every url matching it.
/// </summary>
public static class IgnorePattern
{
    /// <summary>
    /// Whether an ignored url is a pattern. Something other than wildcards and slashes is required after the
    /// host, as bots request /* literally, so a row like that, or https://example.com/* with LogWithHostname
    /// on, can be saved already, and it would stop all suggestions.
    /// </summary>
    public static bool IsPattern(string url)
    {
        return !string.IsNullOrEmpty(url)
               && url.Contains('*')
               && GetPathToCheck(url).IndexOfAnyExcept('*', '/') >= 0;
    }

    /// <summary>
    /// Whether a pattern is compared to the whole url, such as https://example.com/api/*. Any other pattern is
    /// compared to the path and query, so the scheme and host can't make *.com* match every request.
    /// </summary>
    public static bool IsAbsolute(string pattern)
    {
        return pattern.StartsWith("http", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsMatch(string pattern, string url)
    {
        return FileSystemName.MatchesSimpleExpression(HideMatcherSyntax(pattern), HideMatcherSyntax(url));
    }

    /// <summary>
    /// * is the only wildcard. To the matcher ? is one too, which would make /?* match everything, and \ escapes,
    /// while a ? in a url starts the query string, and both are literal when deleting suggestions. A ? can't be
    /// escaped for the matcher, even with a \, so both are swapped for private use characters on either side.
    /// </summary>
    private static string HideMatcherSyntax(string value)
    {
        return value.Replace('?', '\uE000').Replace('\\', '\uE001');
    }

    private static ReadOnlySpan<char> GetPathToCheck(string url)
    {
        if (!IsAbsolute(url))
        {
            return url;
        }

        var schemeEnd = url.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd < 0)
        {
            return ReadOnlySpan<char>.Empty;
        }

        var hostStart = schemeEnd + "://".Length;
        var pathStart = url.IndexOf('/', hostStart);

        return pathStart < 0 ? ReadOnlySpan<char>.Empty : url.AsSpan(pathStart);
    }
}
