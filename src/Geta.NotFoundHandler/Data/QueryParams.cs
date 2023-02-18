// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Data.SqlClient;
using Geta.NotFoundHandler.Core.Redirects;
using Microsoft.AspNetCore.Mvc;

namespace Geta.NotFoundHandler.Data
{
    public class QueryParams
    {
        [BindProperty(Name = "q", SupportsGet = true)]
        public string QueryText { get; set; } = string.Empty;

        public RedirectState? QueryState { get; set; }

        [BindProperty(Name = "p", SupportsGet = true)]
        public int Page { get; set; } = 1;

        [BindProperty(Name = "ps", SupportsGet = true)]
        public int? PageSize { get; set; }

        [BindProperty(Name = "sb", SupportsGet = true)]
        public string SortBy { get; set; }

        [BindProperty(Name = "sd", SupportsGet = true)]
        public SortOrder SortDirection { get; set; } = SortOrder.Ascending;
    }
}
