using elFinder.Net.AspNetCore.Extensions;
using elFinder.Net.Drivers.FileSystem.Extensions;
using Grand.Infrastructure;
using Grand.Web.Admin.Infrastructure;
using Grand.Web.Common.View;

namespace Grand.Web.Admin.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAreaViewFactory, AdminAreaViewFactory>();
        // IAdminDataScope<Product> is registered once, centrally, by Grand.Web.AdminShared's own
        // StartupApplication via RoutedProductDataScope - see its doc comment. Registering it here
        // too would race with Store's/Vendor's registrations under the combined Grand.Web host.
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 101;
    public bool BeforeConfigure => false;
}