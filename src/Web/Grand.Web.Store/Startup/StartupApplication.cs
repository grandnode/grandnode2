using Grand.Data;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Store.Services;

namespace Grand.Web.Store.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        if (!DataSettingsManager.DatabaseIsInstalled())
            return;

        // Register Store-specific implementation of IPageViewModelService
        services.AddScoped<IPageViewModelService, StorePageViewModelService>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 103;
    public bool BeforeConfigure => false;
}
