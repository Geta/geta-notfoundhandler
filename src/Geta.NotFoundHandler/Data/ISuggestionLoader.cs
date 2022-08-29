// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Geta.NotFoundHandler.Core.Suggestions;

namespace Geta.NotFoundHandler.Data
{
    public interface ISuggestionLoader
    {
        IEnumerable<SuggestionSummary> GetAllSummaries();

        IEnumerable<SuggestionSummary> GetSummariesPaged(int page, int pageSize);
    }
}
