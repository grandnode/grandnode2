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
        services.AddScoped<IProductViewModelService, ProductViewModelService>();
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