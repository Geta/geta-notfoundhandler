using EPiServer.Authorization;
using EPiServer.Framework.Hosting;
using EPiServer.Web.Hosting;
using Geta.NotFoundHandler.Infrastructure.Configuration;
using Geta.NotFoundHandler.Infrastructure.Initialization;
using Geta.NotFoundHandler.Optimizely;
using Geta.NotFoundHandler.Optimizely.Infrastructure.Configuration;
using Geta.NotFoundHandler.Optimizely.Infrastructure.Initialization;

namespace Geta.NotFoundHandler.Web;

public class Startup
{
    private readonly IConfiguration _configuration;
    private readonly Foundation.Startup _foundationStartup;

    public Startup(IWebHostEnvironment webHostingEnvironment, IConfiguration configuration)
    {
        _configuration = configuration;
        _foundationStartup = new Foundation.Startup(webHostingEnvironment, configuration);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddNotFoundHandler(o => o.UseSqlServer(_configuration.GetConnectionString("EPiServerDB")),
                            policy => policy.RequireRole(Roles.CmsAdmins));
        services.AddOptimizelyNotFoundHandler();
        _foundationStartup.ConfigureServices(services);

        var moduleName = typeof(ContainerController).Assembly.GetName().Name;
        var fullPath = Path.GetFullPath($"../{moduleName}");

        services.Configure<CompositeFileProviderOptions>(options =>
        {
            options.BasePathFileProviders.Add(new MappingPhysicalFileProvider(
                                                  $"/EPiServer/{moduleName}",
                                                  string.Empty,
                                                  fullPath));
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseNotFoundHandler();
        app.UseOptimizelyNotFoundHandler();

        _foundationStartup.Configure(app, env);
    }
}
