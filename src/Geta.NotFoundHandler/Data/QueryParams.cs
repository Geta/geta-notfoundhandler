// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Data.SqlClient;
using Geta.NotFoundHandler.Core.Redirects;
using Microsoft.AspNetCore.Mvc;

namespace Geta.NotFoundHandler.Data
{
    public class QueryParams
    {
        [FromForm(Name = "q")]
        public string QueryText { get; set; } = string.Empty;

        public RedirectState? QueryState { get; set; }

        [FromForm(Name = "page")]
        public int Page { get; set; } = 1;

        [FromForm(Name = "page-size")]
        public int? PageSize { get; set; }

        [FromForm(Name = "sort-by")]
        public string SortBy { get; set; }

        [FromForm(Name = "sort-direction")]
        public SortOrder SortDirection { get; set; } = SortOrder.Ascending;
    }
}
