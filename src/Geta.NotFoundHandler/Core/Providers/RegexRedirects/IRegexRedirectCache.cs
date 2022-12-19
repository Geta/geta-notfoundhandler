// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;

namespace Geta.NotFoundHandler.Core.Providers.RegexRedirects;

public interface IRegexRedirectCache
{
    IEnumerable<RegexRedirect> GetOrCreate();
    void Remove();
}
