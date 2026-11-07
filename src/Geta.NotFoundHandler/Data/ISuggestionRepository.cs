// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;

namespace Geta.NotFoundHandler.Data
{
    public interface ISuggestionRepository
    {
        void DeleteAll();
        void Delete(int maxErrors, int minimumDaysOld);
        void DeleteForRequest(string oldUrl);

        /// <summary>
        /// Deletes the suggestions for the urls matching a pattern with * as the wildcard, such as /api/*.
        /// </summary>
        void DeleteMatching(string pattern)
        {
        }

        void Save(string oldUrl, string referer, DateTime requestedOn);
    }
}
