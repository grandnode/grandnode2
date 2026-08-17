using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.Vendor.Interfaces;
using Grand.Web.Vendor.Services;

namespace Grand.Web.Vendor.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        if (!DataSettingsManager.DatabaseIsInstalled())
            return;

        services.AddScoped<Grand.Web.AdminShared.Interfaces.IAdminDataScope<Product>, Grand.Web.AdminShared.Services.VendorProductDataScope>();
        // IProductViewModelService is registered by Grand.Web.AdminShared's own StartupApplication
        // (Grand.Web.AdminShared/Startup/StartupApplication.cs), which is discovered and run for this
        // host too via the IStartupApplication assembly scan in StartupBase, since Vendor references
        // AdminShared. Registering it again here would just be a redundant duplicate of that line.
        services.AddScoped<IOrderViewModelService, OrderViewModelService>();
        services.AddScoped<IShipmentViewModelService, ShipmentViewModelService>();
        services.AddScoped<IMerchandiseReturnViewModelService, MerchandiseReturnViewModelService>();
        services.AddScoped<IVendorReviewViewModelService, VendorReviewViewModelService>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 102;
    public bool BeforeConfigure => false;
}