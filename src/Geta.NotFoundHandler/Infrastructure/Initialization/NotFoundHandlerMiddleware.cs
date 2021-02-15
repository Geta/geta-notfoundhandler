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