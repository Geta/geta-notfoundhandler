// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Net;
using Geta.NotFoundHandler.Core.Redirects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Geta.NotFoundHandler.Infrastructure.Web
{
    public static class HttpContextExtensions
    {
        private const string NullIpAddress = "::1";
        private static bool? _isLocalRequest;

        public static bool IsLocalRequest(this HttpContext httpContext)
        {
            if (_isLocalRequest.HasValue) return _isLocalRequest.Value;

            var connection = httpContext.Connection;

            _isLocalRequest = !connection.RemoteIpAddress.IsSet() || (connection.LocalIpAddress.IsSet()
                                  //If local is same as remote, then we are local
                                  ? connection.RemoteIpAddress.Equals(connection.LocalIpAddress)
                                  //else we are remote if the remote IP address is not a loopback address
                                  : IPAddress.IsLoopback(connection.RemoteIpAddress));

            return _isLocalRequest.Value;
        }

        private static bool IsSet(this IPAddress address)
        {
            return address != null && address.ToString() != NullIpAddress;
        }

        public static HttpContext SetStatusCode(this HttpContext context, int statusCode)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = statusCode;
            }
            
            return context;
        }

        public static HttpContext Redirect(this HttpContext context, string url, RedirectType redirectType)
        {
            context.Response.Clear();

            var permanent = redirectType == RedirectType.Permanent;

            if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
            {
                url = UriHelper.Encode(uri);
            }

            context.Response.Redirect(url, permanent);
            
            return context;
        }
    }
}
