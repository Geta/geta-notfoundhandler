// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public interface IContentUrlHistoryLoader
    {
        bool IsRegistered(ContentUrlHistory entity);
        IEnumerable<(string contentKey, IReadOnlyCollection<ContentUrlHistory> histories)> GetAllMoved();
        IReadOnlyCollection<ContentUrlHistory> GetMoved(string contentKey);
    }
}
