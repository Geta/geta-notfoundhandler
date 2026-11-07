// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Linq;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Core.Suggestions;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Geta.NotFoundHandler.Infrastructure.Web;
using Microsoft.AspNetCore.Diagnostics;
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
        private readonly RedirectLoopGuard _redirectLoopGuard;
        private const string HandledRequestItemKey = "NotFoundHandler:handled";

        public RequestHandler(
            IRedirectHandler redirectHandler,
            IRequestLogger requestLogger,
            IOptions<NotFoundHandlerOptions> options,
            ILogger<RequestHandler> logger,
            RedirectLoopGuard redirectLoopGuard)
        {
            _configuration = options.Value;
            _requestLogger = requestLogger ?? throw new ArgumentNullException(nameof(requestLogger));
            _logger = logger;
            _redirectLoopGuard = redirectLoopGuard ?? throw new ArgumentNullException(nameof(redirectLoopGuard));
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

            if (_configuration.ActiveStatusCodes.Any() && !_configuration.ActiveStatusCodes.Contains(context.Response.StatusCode))
            {
                LogDebug("Not a accepted statuscode.", context);
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

            var notFoundUri = GetNotFoundUri(context);

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

            MarkHandled(context);
        }

        /// <summary>
        /// Gets the URL the client asked for. When a status code page is being re-executed,
        /// the current request is the error page, so the original URL has to be used instead.
        /// </summary>
        private static Uri GetNotFoundUri(HttpContext context)
        {
            var request = context.Request;
            var reExecuteFeature = context.Features.Get<IStatusCodeReExecuteFeature>();

            return reExecuteFeature == null
                ? new Uri(request.GetDisplayUrl())
                : new Uri($"{request.Scheme}://{request.Host}{reExecuteFeature.OriginalPathBase}{reExecuteFeature.OriginalPath}{reExecuteFeature.OriginalQueryString}");
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
                    && string.Compare(redirect.NewUrl, urlNotFound.PathAndQuery, StringComparison.InvariantCultureIgnoreCase) != 0
                    && string.Compare(redirect.NewUrl, urlNotFound.AbsoluteUri, StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    // Found it, however, we need to make sure we're not running in an
                    // infinite loop. The new url must not be the referer to this page
                    if (!_redirectLoopGuard.TryRedirect(urlNotFound, redirect.NewUrl))
                    {
                        _logger.LogWarning(
                            "Stopped redirecting {Url} to {NewUrl} after {MaxRedirectHops} redirects in a row. Check the redirects for a loop.",
                            urlNotFound.PathAndQuery,
                            redirect.NewUrl,
                            _configuration.MaxRedirectHops);
                        return false;
                    }

                    foundRedirect = redirect;
                    return true;
                }
            }
            else
            {
                // Safe logging
                var logUrl = _configuration.LogWithHostname ? urlNotFound.ToString() : urlNotFound.PathAndQuery;

                if (_requestLogger.ShouldLogRequest(logUrl))
                {
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
            if (_configuration.IsIgnoredResource(notFoundUri.AbsolutePath))
            {
                // Ignoring 404 rewrite of known resource extension
                _logger.LogDebug("Ignoring rewrite of '{NotFoundUrl}', as it has a known resource extension",
                                 notFoundUri.ToString());
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
