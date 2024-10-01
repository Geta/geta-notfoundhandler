// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Geta.NotFoundHandler.Core.Redirects;

namespace Geta.NotFoundHandler.Core;

public class CustomRedirectsResult
{
    public CustomRedirectsResult(IList<CustomRedirect> redirects, int unfilteredCount, int totalCount)
    {
        Redirects = redirects;
        UnfilteredCount = unfilteredCount;
        TotalCount = totalCount;
    }

    public IList<CustomRedirect> Redirects { get; init; }

    public int UnfilteredCount { get; init; }

    public int TotalCount { get; init; }
}
