// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Geta.NotFoundHandler.Core.Providers.RegexRedirects;

namespace Geta.NotFoundHandler.Data;

public interface IRegexRedirectLoader
{
    IEnumerable<RegexRedirect> GetAll();
}
