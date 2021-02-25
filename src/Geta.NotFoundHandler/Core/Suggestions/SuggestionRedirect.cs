// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

namespace Geta.NotFoundHandler.Core.Suggestions
{
    public class SuggestionRedirect
    {
        public string OldUrl { get; }
        public string NewUrl { get; }

        public SuggestionRedirect(string oldUrl, string newUrl)
        {
            OldUrl = oldUrl;
            NewUrl = newUrl;
        }
    }
}
