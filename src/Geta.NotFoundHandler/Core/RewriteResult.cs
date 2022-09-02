// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Geta.NotFoundHandler.Core.Redirects;

namespace Geta.NotFoundHandler.Core;

public class RewriteResult
{
    public static readonly RewriteResult Empty = new() { IsEmpty = true };
    public string NewUrl { get; }
    public RedirectType RedirectType { get; }
    public bool IsEmpty { get; private set; }

    private RewriteResult() { }

    public RewriteResult(string newUrl, RedirectType redirectType)
    {
        NewUrl = newUrl;
        RedirectType = redirectType;
    }
}
