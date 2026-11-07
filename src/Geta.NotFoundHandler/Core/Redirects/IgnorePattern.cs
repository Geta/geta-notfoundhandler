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
    /// Whether an ignored url is a pattern. Something other than wildcards and slashes is required, as bots
    /// request /* literally, so a row like that can be saved already, and it would stop all suggestions.
    /// </summary>
    public static bool IsPattern(string url)
    {
        return !string.IsNullOrEmpty(url)
               && url.Contains('*')
               && url.AsSpan().IndexOfAnyExcept('*', '/') >= 0;
    }

    public static bool IsMatch(string pattern, string url)
    {
        return FileSystemName.MatchesSimpleExpression(pattern, url);
    }
}
