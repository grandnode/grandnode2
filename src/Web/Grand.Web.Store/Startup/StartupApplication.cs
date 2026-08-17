using Grand.Infrastructure;

namespace Grand.Web.Store.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // IAdminDataScope<Product> is registered once, centrally, by Grand.Web.AdminShared's own
        // StartupApplication via RoutedProductDataScope - see its doc comment. Registering it here
        // too would race with Admin's/Vendor's registrations under the combined Grand.Web host.
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 101;
    public bool BeforeConfigure => false;
}
