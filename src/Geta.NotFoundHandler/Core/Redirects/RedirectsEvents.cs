// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;

namespace Geta.NotFoundHandler.Core.Redirects
{
    public class RedirectsEvents
    {
        public event EventHandler OnRedirectsUpdated;
        public event EventHandler OnRegexRedirectsUpdated;

        public void RedirectsUpdated()
        {
            Raise(OnRedirectsUpdated);
        }

        public void RegexRedirectsUpdated()
        {
            Raise(OnRegexRedirectsUpdated);
        }

        /// <summary>
        /// Calls every handler even if one fails, so a failed reload on this instance
        /// doesn't stop the other instances from being told about the update.
        /// </summary>
        private static void Raise(EventHandler handlers)
        {
            if (handlers == null)
            {
                return;
            }

            var args = new EventArgs();
            var failures = new List<Exception>();

            foreach (var handler in handlers.GetInvocationList().Cast<EventHandler>())
            {
                try
                {
                    handler(args);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            }

            if (failures.Count > 0)
            {
                throw new AggregateException(failures);
            }
        }
    }

    public delegate void EventHandler(EventArgs e);
}
