// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;

namespace Geta.NotFoundHandler.Core.Redirects
{
    /// <summary>
    /// Handler for custom redirects. Loads and caches the list of custom redirects
    /// to ensure performance.
    /// </summary>
    public class CustomRedirectHandler : IRedirectHandler
    {
        private static readonly object _lock = new object();
        private CustomRedirectCollection _customRedirects = new CustomRedirectCollection();

        public CustomRedirect Find(Uri urlNotFound)
        {
            return _customRedirects.Find(urlNotFound);
        }

        public void Set(CustomRedirectCollection redirects)
        {
            lock (_lock)
            {
                _customRedirects = redirects;
            }
        }
    }
}
