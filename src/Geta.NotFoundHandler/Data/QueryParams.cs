// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Data.SqlClient;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Infrastructure.Web;

namespace Geta.NotFoundHandler.Data
{
    public class QueryParams
    {
        [FromFormOrQuery(Name = "q")]
        public string QueryText { get; set; } = string.Empty;

        public RedirectState? QueryState { get; set; }

        [FromFormOrQuery(Name = "page")]
        public int Page { get; set; } = 1;

        [FromFormOrQuery(Name = "page-size")]
        public int? PageSize { get; set; }

        [FromFormOrQuery(Name = "sort-by")]
        public string SortBy { get; set; }

        [FromFormOrQuery(Name = "sort-direction")]
        public SortOrder SortDirection { get; set; } = SortOrder.Ascending;
    }
}
