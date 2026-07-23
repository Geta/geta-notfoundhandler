// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Geta.NotFoundHandler.Core;
using Microsoft.AspNetCore.Http;

namespace Geta.NotFoundHandler.Infrastructure.Initialization
{
    public class NotFoundHandlerMiddleware : IMiddleware
    {
        private readonly RequestHandler _requestHandler;

        public NotFoundHandlerMiddleware(RequestHandler requestHandler)
        {
            _requestHandler = requestHandler;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            context.Response.OnStarting(state =>
            {
                _requestHandler.Handle((HttpContext)state);
                return Task.CompletedTask;
            }, context);

            await next(context);
        }
    }
}
