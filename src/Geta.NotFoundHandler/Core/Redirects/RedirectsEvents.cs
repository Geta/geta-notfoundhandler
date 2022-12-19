// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;

namespace Geta.NotFoundHandler.Core.Redirects
{
    public class RedirectsEvents
    {
        public event EventHandler OnRedirectsUpdated;
        public event EventHandler OnRegexRedirectsUpdated;

        public void RedirectsUpdated()
        {
            OnRedirectsUpdated?.Invoke(new EventArgs());
        }

        public void RegexRedirectsUpdated()
        {
            OnRegexRedirectsUpdated?.Invoke(new EventArgs());
        }
    }

    public delegate void EventHandler(EventArgs e);
}
