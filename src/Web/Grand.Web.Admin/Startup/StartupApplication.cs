using elFinder.Net.AspNetCore.Extensions;
using elFinder.Net.Drivers.FileSystem.Extensions;
using Grand.Infrastructure;
using Grand.Web.Admin.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.View;

namespace Grand.Web.Admin.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAreaViewFactory, AdminAreaViewFactory>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 101;
    public bool BeforeConfigure => false;
}