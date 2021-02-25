// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;

namespace Geta.NotFoundHandler.Core.Suggestions
{
    public class SuggestionSummary
    {
        public string OldUrl { get; set; }
        public int Count { get; set; }
        public ICollection<RefererSummary> Referers { get; set; }
    }
}
