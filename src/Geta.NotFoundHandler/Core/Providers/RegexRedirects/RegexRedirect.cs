// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Text.RegularExpressions;

namespace Geta.NotFoundHandler.Core.Providers.RegexRedirects;

public class RegexRedirect
{
    public RegexRedirect(Regex oldUrlRegex, string newUrlFormat)
    {
        OldUrlRegex = oldUrlRegex;
        NewUrlFormat = newUrlFormat;
    }

    public RegexRedirect(Guid id, Regex oldUrlRegex, string newUrlFormat)
    {
        Id = id;
        OldUrlRegex = oldUrlRegex;
        NewUrlFormat = newUrlFormat;
    }

    public Guid? Id { get; }
    public Regex OldUrlRegex { get; }
    public string NewUrlFormat { get; }
}
