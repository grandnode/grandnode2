using Grand.Data;
using Grand.Infrastructure;

namespace Grand.Web.Vendor.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        if (!DataSettingsManager.DatabaseIsInstalled())
            return;

    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 102;
    public bool BeforeConfigure => false;
}