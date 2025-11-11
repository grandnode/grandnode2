using Grand.Data;
using Grand.Infrastructure;
using Grand.Web.Store.Interfaces;
using Grand.Web.Store.Services;

namespace Grand.Web.Store.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        if (!DataSettingsManager.DatabaseIsInstalled())
            return;

        // Register Store-specific extended implementation of IPageViewModelService
        services.AddScoped<IStorePageViewModelService, StorePageViewModelService>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 103;
    public bool BeforeConfigure => false;
}
