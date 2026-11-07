// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using Geta.NotFoundHandler.Core.Redirects;

namespace Geta.NotFoundHandler.Core.Providers.RegexRedirects;

public interface IRegexRedirectsService
{
    void Create(string oldUrlRegex, string newUrlFormat, int orderNumber);

    /// <param name="redirectType">Null follows the configured default redirect type.</param>
    void Create(string oldUrlRegex, string newUrlFormat, int orderNumber, RedirectType? redirectType);

    /// <summary>
    /// Updates a rule, keeping its redirect type.
    /// </summary>
    void Update(Guid id, string oldUrlRegex, string newUrlFormat, int orderNumber);

    /// <param name="redirectType">Null follows the configured default redirect type.</param>
    void Update(Guid id, string oldUrlRegex, string newUrlFormat, int orderNumber, RedirectType? redirectType);
    void Delete(Guid id);
}
