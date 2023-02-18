// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Geta.NotFoundHandler.Data;

namespace Geta.NotFoundHandler.Core.Redirects
{
    public interface IRedirectsService
    {
        IEnumerable<CustomRedirect> GetAll();

        [Obsolete($"Use {nameof(GetRedirects)} instead")]
        IEnumerable<CustomRedirect> GetSaved();

        [Obsolete($"Use {nameof(GetRedirects)} instead")]
        IEnumerable<CustomRedirect> GetIgnored();

        [Obsolete($"Use {nameof(GetRedirects)} instead")]
        IEnumerable<CustomRedirect> GetDeleted();

        [Obsolete($"Use {nameof(GetRedirects)} instead")]
        IEnumerable<CustomRedirect> Search(string searchText);

        CustomRedirectsResult GetRedirects(QueryParams query);

        void AddOrUpdate(CustomRedirect redirect);

        void AddOrUpdate(IEnumerable<CustomRedirect> redirects);

        void AddDeletedRedirect(string oldUrl);

        void DeleteByOldUrl(string oldUrl);

        void DeleteByOldUrl(IEnumerable<string> oldUrls);

        int DeleteAll();

        int DeleteAllIgnored();
    }
}
