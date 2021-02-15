using EPiServer.Logging;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;

namespace Geta.NotFoundHandler.Infrastructure.Initialization
{
    public static class ApplicationBuilderExtensions
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        public static IApplicationBuilder UseNotFoundHandler(this IApplicationBuilder app)
        {
            Logger.Debug("Initializing NotFoundHandler version check");
            var dba = DataAccessBaseEx.GetWorker();
            var version = dba.CheckNotFoundHandlerVersion();
            if (version != NotFoundHandlerOptions.CurrentDbVersion)
            {
                Logger.Debug("Older version found. Version nr. :" + version);
                Upgrader.Start(version);
            }
            else
            {
                Upgrader.Valid = true;
            }

            // Load all custom redirects into memory
            // TODO: create better load of the cache (init in a hosted service) https://andrewlock.net/running-async-tasks-on-app-startup-in-asp-net-core-3/
            var handler = CustomRedirectHandler.Current;

            app.UseMiddleware<NotFoundHandlerMiddleware>();

            return app;
        }
    }
}