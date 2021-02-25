// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;

namespace Geta.NotFoundHandler.Core.Redirects
{
    public class RedirectsEvents
    {
        public event EventHandler OnUpdated;

        public void RedirectsUpdated()
        {
            OnUpdated?.Invoke(new EventArgs());
        }
    }

    public delegate void EventHandler(EventArgs e);
}
