using EPiServer.Logging;
using Geta.NotFoundHandler.Core.Redirects;
using Geta.NotFoundHandler.Data;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

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
            
            // Load all custom redirects into memory
            var services = app.ApplicationServices;

            var initializer = services.GetRequiredService<RedirectsInitializer>();
            initializer.Initialize();

            app.UseMiddleware<NotFoundHandlerMiddleware>();

            return app;
        }
    }
}
