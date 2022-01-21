using AlloyMvcTemplates.Extensions;
using AlloyMvcTemplates.Infrastructure;
using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.Data;
using EPiServer.Framework.Web.Resources;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using AlloyMvcTemplates;
using EPiServer.Authorization;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Geta.NotFoundHandler.Infrastructure.Initialization;
using Geta.NotFoundHandler.Optimizely;

namespace EPiServer.Templates.Alloy.Mvc
{
    public class Startup
    {
        private readonly IWebHostEnvironment _webHostingEnvironment;
        private readonly IConfiguration _configuration;

        public Startup(IWebHostEnvironment webHostingEnvironment, IConfiguration configuration)
        {
            _webHostingEnvironment = webHostingEnvironment;
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = _configuration.GetConnectionString("EPiServerDB")
                .Replace("App_Data", Path.GetFullPath("App_Data"));
            if (_webHostingEnvironment.IsDevelopment())
            {
                services.Configure<SchedulerOptions>(o =>
                {
                    o.Enabled = false;
                });

                services.PostConfigure<DataAccessOptions>(o =>
                {
                    o.SetConnectionString(connectionString);
                });
                services.PostConfigure<ApplicationOptions>(o =>
                {
                    o.ConnectionStringOptions.ConnectionString = connectionString;
                });
            }

            services.AddCmsAspNetIdentity<ApplicationUser>();
            services.AddMvc();
            services.AddAlloy();
            services.AddCms();

            services.AddNotFoundHandler(o =>
            {
                o.UseSqlServer(connectionString);
                o.AddProvider<NullNotFoundHandlerProvider>();
            }, policy =>
            {
                policy.RequireRole(Roles.CmsAdmins);
            });
            services.AddOptimizelyNotFoundHandler();

            services.AddEmbeddedLocalization<Startup>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseNotFoundHandler();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMiddleware<AdministratorRegistrationPageMiddleware>();
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapContent();
                endpoints.MapControllerRoute("Register", "/Register", new { controller = "Register", action = "Index" });
                endpoints.MapRazorPages();
            });
        }
    }
}
