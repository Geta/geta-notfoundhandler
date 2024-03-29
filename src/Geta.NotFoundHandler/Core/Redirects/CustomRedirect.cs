// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;

namespace Geta.NotFoundHandler.Core.Redirects
{
    public class CustomRedirect
    {
        /// <summary>
        /// Gets or sets a value indicating whether to skip appending the
        /// old url fragment to the new one. Default value is false.
        /// </summary>
        /// <remarks>
        /// If you want to redirect many addresses below a specific one to
        /// one new url, set this to true. If we get a wild card match on
        /// this url, the new url will be used in its raw format, and the
        /// old url will not be appended to the new one.
        /// </remarks>
        /// <value><c>true</c> to skip appending old url if wild card match; otherwise, <c>false</c>.</value>
        public bool WildCardSkipAppend { get; set; }

        private string _oldUrl;
        public string OldUrl
        {
            get => _oldUrl;
            set => _oldUrl = value?.ToLower();
        }

        public string NewUrl { get; set; }

        public int  State { get; set; }

        // 301 (permanent) or 302 (temporary)
        public RedirectType RedirectType { get; set; }

        public Guid? Id { get; set; }

        public CustomRedirect()
        {
        }

        public CustomRedirect(string oldUrl, string newUrl, bool skipWildCardAppend, RedirectType redirectType)
            : this(oldUrl, newUrl)
        {
            WildCardSkipAppend = skipWildCardAppend;
            RedirectType = redirectType;
        }

        public CustomRedirect(string oldUrl, RedirectState state)
            :this(oldUrl, string.Empty)
        {
            State = Convert.ToInt32(state);
        }

        public CustomRedirect(string oldUrl, string newUrl)
        {
            OldUrl = oldUrl;
            NewUrl = newUrl;
        }

        public CustomRedirect(CustomRedirect redirect)
        {
            OldUrl = redirect._oldUrl;
            NewUrl = redirect.NewUrl;
            WildCardSkipAppend = redirect.WildCardSkipAppend;
            RedirectType = redirect.RedirectType;
        }   
    }
}
