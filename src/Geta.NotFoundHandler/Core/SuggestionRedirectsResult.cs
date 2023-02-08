// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Geta.NotFoundHandler.Core.Suggestions;

namespace Geta.NotFoundHandler.Core;

public class SuggestionRedirectsResult
{
    public SuggestionRedirectsResult(IList<SuggestionSummary> redirects, int unfilteredCount, int totalCount)
    {
        Suggestions = redirects;
        UnfilteredCount = unfilteredCount;
        TotalCount = totalCount;
    }

    public IList<SuggestionSummary> Suggestions { get; init; }

    public int UnfilteredCount { get; init; }

    public int TotalCount { get; init; }
}
