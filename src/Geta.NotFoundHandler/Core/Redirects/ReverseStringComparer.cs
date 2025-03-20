// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;

namespace Geta.NotFoundHandler.Core.Redirects;

/// <summary>
/// A comparer that sorts strings in reverse order.
/// </summary>
public class ReverseStringComparer : IComparer<string>
{
    private readonly IComparer<string> _baseComparer;

    public ReverseStringComparer() : this(StringComparer.OrdinalIgnoreCase) { }

    public ReverseStringComparer(IComparer<string> baseComparer)
    {
        _baseComparer = baseComparer ?? throw new ArgumentNullException(nameof(baseComparer));
    }

    public int Compare(string x, string y)
    {
        return _baseComparer.Compare(y, x);
    }
}
