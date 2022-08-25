using System;

namespace Geta.NotFoundHandler.Infrastructure.Processing
{
    internal static class SpanExtensions
    {
        public static ReadOnlySpan<char> AsPathSpan(this string url)
        {
            var index = url.IndexOf('?');
            if (index >= 0)
                return url.AsSpan(0, index);

            return url.AsSpan();
        }

        public static ReadOnlySpan<char> AsQuerySpan(this string url)
        {
            var index = url.IndexOf('?');
            if (index >= 0)
                return url.AsSpan(index);

            return ReadOnlySpan<char>.Empty;
        }

        public static ReadOnlySpan<char> RemoveLeadingSlash(this ReadOnlySpan<char> chars)
        {
            if (chars.StartsWith("/"))
                return chars[1..];

            return chars;
        }

        public static ReadOnlySpan<char> RemoveTrailingSlash(this ReadOnlySpan<char> chars)
        {
            if (chars.EndsWith("/"))
                return chars[..^1];

            return chars;
        }

        public static bool UrlPathMatch(this ReadOnlySpan<char> path, ReadOnlySpan<char> otherPath)
        {
            otherPath = RemoveTrailingSlash(otherPath);

            if (path.Length < otherPath.Length)
                return false;

            for (var i = 0; i < otherPath.Length; i++)
            {
                var currentChar = char.ToLowerInvariant(path[i]);
                var otherChar = char.ToLowerInvariant(otherPath[i]);

                if (!currentChar.Equals(otherChar))
                    return false;
            }

            if (path.Length == otherPath.Length)
                return true;

            return path[otherPath.Length] == '/';
        }
    }
}
