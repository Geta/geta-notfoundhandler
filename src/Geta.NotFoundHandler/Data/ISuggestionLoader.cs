// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Suggestions;
using X.PagedList;

namespace Geta.NotFoundHandler.Data
{
    public interface ISuggestionLoader
    {
        [Obsolete($"Please use other overload for {nameof(GetSummaries)}")]
        IPagedList<SuggestionSummary> GetSummaries(int page, int pageSize);

        SuggestionRedirectsResult GetSummaries(QueryParams query);
    }
}
