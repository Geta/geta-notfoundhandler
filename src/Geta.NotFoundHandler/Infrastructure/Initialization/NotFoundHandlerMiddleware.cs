// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using Geta.NotFoundHandler.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Geta.NotFoundHandler.Infrastructure.Initialization
{
    public class NotFoundHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        public NotFoundHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            RequestHandler requestHandler,
            ILogger<NotFoundHandlerMiddleware> logger)
        {
            // Headers can no longer be changed once the application starts writing the body,
            // so a response that starts inside the pipeline has to be handled just before that.
            if (!context.Response.HasStarted)
            {
                context.Response.OnStarting(() =>
                {
                    try
                    {
                        requestHandler.Handle(context);
                    }
                    catch (Exception ex)
                    {
                        // No middleware can catch an exception from this callback. The server would
                        // abort the response the application is writing, so keep that response instead.
                        logger.LogError(ex, "Could not handle the request before the response started");
                    }

                    return Task.CompletedTask;
                });
            }

            await _next(context);

            // Otherwise handle it now instead of waiting for the response to start, so that middleware
            // registered before this one sees the result: a redirect or 410 instead of the 404.
            if (!context.Response.HasStarted)
            {
                requestHandler.Handle(context);
            }
        }
    }
}
