// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;

namespace Geta.NotFoundHandler.Core.Providers.RegexRedirects;

public interface IRegexRedirectsService
{
    void Create(string oldUrlRegex, string newUrlFormat, int orderNumber);
    void Update(Guid id, string oldUrlRegex, string newUrlFormat, int orderNumber);
    void Delete(Guid id);
}
