// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class ContentKeyResult
    {
        public static ContentKeyResult Empty { get; } = new();

        public string Key { get; }
        public bool HasValue { get; }

        private ContentKeyResult() { }

        public ContentKeyResult(string key)
        {
            Key = key;
            HasValue = true;
        }
    }
}
