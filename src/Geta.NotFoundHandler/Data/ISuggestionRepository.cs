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
        void Save(string oldUrl, string referer, DateTime requestedOn);
    }
}
