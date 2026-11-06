// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text;
using Geta.NotFoundHandler.Core.Redirects;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Geta.NotFoundHandler.Tests
{
    public class RedirectsCsvParserTests
    {
        private readonly RedirectsCsvParser _sut = new(NullLogger<RedirectsCsvParser>.Instance);

        /// <summary>
        /// https://github.com/Geta/geta-notfoundhandler/issues/174
        /// </summary>
        [Theory]
        [InlineData("=1+1")]
        [InlineData("=cmd|' /C calc'!A0")]
        [InlineData("@SUM(1+1)")]
        [InlineData("+1+1")]
        [InlineData("-1+1")]
        [InlineData("\t=1+1")]
        [InlineData("\r=1+1")]
        [InlineData("\n=1+1")]
        public void ExportToCsv_escapes_values_a_spreadsheet_would_run_as_a_formula(string url)
        {
            var row = ExportRow(url, "/new");

            Assert.StartsWith("\"'" + url[0], row);
        }

        [Fact]
        public void ExportToCsv_escapes_formula_in_new_url()
        {
            var row = ExportRow("/old", "=1+1");

            Assert.Contains(",\"'=1+1\",", row);
        }

        [Theory]
        [InlineData("/about")]
        [InlineData("https://example.com/a?b=1")]
        [InlineData("/om-oss/påske")]
        public void ExportToCsv_leaves_ordinary_urls_unchanged(string url)
        {
            var row = ExportRow(url, "/new");

            Assert.Equal($"{url},/new,False,Permanent", row);
        }

        private string ExportRow(string oldUrl, string newUrl)
        {
            var redirects = new List<CustomRedirect> { new(oldUrl, newUrl, false, RedirectType.Permanent) };

            return Encoding.UTF8.GetString(_sut.ExportToCsv(redirects)).Split("\r\n")[1];
        }
    }
}
