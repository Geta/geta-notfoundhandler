// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public interface IContentUrlHistoryLoader
    {
        bool IsRegistered(ContentUrlHistory entity);
        IEnumerable<(string contentKey, IReadOnlyCollection<ContentUrlHistory> histories)> GetAllMoved();

        /// <summary>
        /// Returns a single page of moved content (keys with more than one URL-history entry),
        /// ordered by content key. Lets callers stream large tables instead of loading every row
        /// in one unbounded query.
        /// </summary>
        /// <param name="skip">Number of moved content keys to skip.</param>
        /// <param name="take">Maximum number of moved content keys to return.</param>
        IEnumerable<(string contentKey, IReadOnlyCollection<ContentUrlHistory> histories)> GetAllMoved(int skip, int take);

        IReadOnlyCollection<ContentUrlHistory> GetMoved(string contentKey);
    }
}
