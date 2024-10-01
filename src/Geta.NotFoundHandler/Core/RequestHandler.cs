// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Linq;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Core.Suggestions;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Geta.NotFoundHandler.Infrastructure.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geta.NotFoundHandler.Core
{
    public class RequestHandler
    {
        private readonly IRedirectHandler _redirectHandler;
        private readonly IRequestLogger _requestLogger;
        private readonly ILogger<RequestHandler> _logger;
        private readonly NotFoundHandlerOptions _configuration;
        private const string HandledRequestItemKey = "NotFoundHandler:handled";

        public RequestHandler(
            IRedirectHandler redirectHandler,
            IRequestLogger requestLogger,
            IOptions<NotFoundHandlerOptions> options,
            ILogger<RequestHandler> logger)
        {
            _configuration = options.Value;
            _requestLogger = requestLogger ?? throw new ArgumentNullException(nameof(requestLogger));
            _logger = logger;
            _redirectHandler = redirectHandler ?? throw new ArgumentNullException(nameof(redirectHandler));
        }

        public virtual void Handle(HttpContext context)
        {
            if (context == null) return;

            if (IsHandled(context))
            {
                LogDebug("Already handled.", context);
                return;
            }

            if (context.Response.StatusCode != 404)
            {
                LogDebug("Not a 404 response.", context);
                return;
            }

            if (_configuration.HandlerMode == FileNotFoundMode.Off)
            {
                LogDebug("Not handled, custom redirect manager is set to off.", context);
                return;
            }

            if (_configuration.HandlerMode == FileNotFoundMode.RemoteOnly)
            {
                if (context.IsLocalRequest())
                {
                    LogDebug("Determined to be localhost, returning.", context);
                    return;
                }

                LogDebug("Not a localhost, handling error.", context);
            }

            LogDebug("Handling 404 request.", context);

            var notFoundUri = new Uri(context.Request.GetDisplayUrl());

            if (IsResourceFile(notFoundUri))
            {
                LogDebug("Skipping resource file.", context);
                return;
            }

            var query = context.Request.QueryString.ToString();

            // avoid duplicate log entries
            if (query.StartsWith("404;"))
            {
                LogDebug("Skipping request with 404; in the query string.", context);
                return;
            }

            var headers = context.Request.GetTypedHeaders();
            var canHandleRedirect = HandleRequest(headers.Referer, notFoundUri, out var newUrl);
            if (canHandleRedirect && newUrl.State == (int)RedirectState.Saved)
            {
                LogDebug("Handled saved URL", context);

                context
                    .Redirect(newUrl.NewUrl, newUrl.RedirectType);
            }
            else if (canHandleRedirect && newUrl.State == (int)RedirectState.Deleted)
            {
                LogDebug("Handled deleted URL", context);

                SetStatusCodeAndShow404(context, 410);
            }
            else
            {
                LogDebug("Not handled. Current URL is ignored or no redirect found.", context);

                SetStatusCodeAndShow404(context);
            }

            MarkHandled(context);
        }

        public static bool IsHandled(HttpContext context)
        {
            return context.Items.Keys.Contains(HandledRequestItemKey)
                   && context.Items[HandledRequestItemKey] != null
                   && (bool)context.Items[HandledRequestItemKey];
        }

        private static void MarkHandled(HttpContext context)
        {
            context.Items[HandledRequestItemKey] = true;
        }

        public virtual bool HandleRequest(Uri referer, Uri urlNotFound, out CustomRedirect foundRedirect)
        {
            var redirect = _redirectHandler.Find(urlNotFound);

            foundRedirect = null;

            if (redirect != null)
            {
                if (redirect.State.Equals((int)RedirectState.Ignored))
                {
                    return false;
                }

                // Url has been deleted from this site
                if (redirect.State.Equals((int)RedirectState.Deleted))
                {
                    foundRedirect = redirect;
                    return true;
                }

                if (redirect.State.Equals((int)RedirectState.Saved)
                    && string.Compare(redirect.NewUrl, urlNotFound.PathAndQuery, StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    // Found it, however, we need to make sure we're not running in an
                    // infinite loop. The new url must not be the referer to this page
                    foundRedirect = redirect;
                    return true;
                }
            }
            else
            {
                // log request to database - if logging is turned on.
                if (_configuration.Logging == LoggerMode.On)
                {
                    // Safe logging
                    var logUrl = _configuration.LogWithHostname ? urlNotFound.ToString() : urlNotFound.PathAndQuery;
                    _requestLogger.LogRequest(logUrl, referer?.ToString());
                }
            }

            return false;
        }

        public virtual void SetStatusCodeAndShow404(HttpContext context, int statusCode = 404)
        {
            context
                .SetStatusCode(statusCode);
        }

        /// <summary>
        /// Determines whether the specified not found URI is a resource file
        /// </summary>
        /// <param name="notFoundUri">The not found URI.</param>
        /// <returns>
        /// <c>true</c> if it is a resource file; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsResourceFile(Uri notFoundUri)
        {
            var extension = notFoundUri.AbsolutePath;
            var extPos = extension.LastIndexOf('.');

            if (extPos <= 0) return false;

            extension = extension.Substring(extPos + 1);
            if (_configuration.IgnoredResourceExtensions.Any(x => x == extension))
            {
                // Ignoring 404 rewrite of known resource extension
                _logger.LogDebug("Ignoring rewrite of '{NotFoundUrl}'. '{Extension}' is a known resource extension",
                                 notFoundUri.ToString(),
                                 extension);
                return true;
            }

            return false;
        }

        private void LogDebug(string message, HttpContext context)
        {
            _logger.LogDebug(
                "{Message} Request URL: {RequestUrl} Response status code: {StatusCode}",
                message,
                context?.Request.Path,
                context?.Response.StatusCode);
        }
    }
}
