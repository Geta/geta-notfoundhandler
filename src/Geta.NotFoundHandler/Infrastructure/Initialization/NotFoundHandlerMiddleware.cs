// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Geta.NotFoundHandler.Core;
using Microsoft.AspNetCore.Http;

namespace Geta.NotFoundHandler.Infrastructure.Initialization
{
    public class NotFoundHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        public NotFoundHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, RequestHandler requestHandler)
        {
            await _next(context);

            requestHandler.Handle(context);
        }
    }
}
