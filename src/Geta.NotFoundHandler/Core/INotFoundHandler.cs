// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using Geta.NotFoundHandler.Core.Redirects;

namespace Geta.NotFoundHandler.Core
{
    /// <summary>
    /// Interface for creating custom redirect handling.
    /// </summary>
    public interface INotFoundHandler
    {
        /// <summary>
        /// Create a redirect url from the old url.
        /// This could for example be done by using Regex.Replace(...)
        /// </summary>
        /// <param name="url">The old url which will be redirected</param>
        /// <returns>The new url for the redirect. If no new url has been created, null should be returned instead.</returns>
        RewriteResult RewriteUrl(string url);
    }

    public class RewriteResult
    {
        public static RewriteResult Empty = new RewriteResult { IsEmpty = true };
        public string NewUrl { get; }
        public RedirectType RedirectType { get; }
        public bool IsEmpty { get; private set; }

        private RewriteResult() { }

        public RewriteResult(string newUrl, RedirectType redirectType)
        {
            NewUrl = newUrl;
            RedirectType = redirectType;
            throw new System.NotImplementedException();
        }
    }
}
