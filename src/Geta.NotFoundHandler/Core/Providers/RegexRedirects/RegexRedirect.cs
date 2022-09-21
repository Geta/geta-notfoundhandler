// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Text.RegularExpressions;

namespace Geta.NotFoundHandler.Core.Providers.RegexRedirects;

public class RegexRedirect
{
    public RegexRedirect(
        Guid? id,
        Regex oldUrlRegex,
        string newUrlFormat,
        int orderNumber,
        int? timeoutCount,
        DateTime? createdAt,
        DateTime? modifiedAt)
    {
        Id = id;
        OldUrlRegex = oldUrlRegex;
        NewUrlFormat = newUrlFormat;
        OrderNumber = orderNumber;
        TimeoutCount = timeoutCount;
        CreatedAt = createdAt;
        ModifiedAt = modifiedAt;
    }

    public Guid? Id { get; }
    public Regex OldUrlRegex { get; }
    public string NewUrlFormat { get; }
    public int OrderNumber { get; }
    public int? TimeoutCount { get; }
    public DateTime? CreatedAt { get; }
    public DateTime? ModifiedAt { get; }
}
