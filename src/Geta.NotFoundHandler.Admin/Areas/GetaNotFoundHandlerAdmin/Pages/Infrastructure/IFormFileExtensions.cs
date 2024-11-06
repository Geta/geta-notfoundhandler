using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Infrastructure
{
    public static class IFormFileExtensions
    {
        public static bool IsXml(this IFormFile file)
        {
            return FileIsOfType(file, new[] { "text/xml", "application/xml" }, new[] { "xml" });
        }

        public static bool IsCsv(this IFormFile file)
        {
            return FileIsOfType(file, new[] { "text/csv" }, new[] { "csv" });
        }

        public static bool IsTxt(this IFormFile file)
        {
            return FileIsOfType(file, new[] { "text/plain" }, new[] { "txt" });
        }

        public static bool FileIsOfType(this IFormFile file, string[] allowedContentTypes, string[] allowedExtensions)
        {
            var isAllowedContentType = allowedContentTypes.Any(
                x => file.ContentType.Equals(x, StringComparison.InvariantCultureIgnoreCase));
            if (isAllowedContentType)
            {
                return true;
            }

            return allowedExtensions.Any(x => file.FileName.EndsWith(x, StringComparison.OrdinalIgnoreCase));
        }
    }
}
