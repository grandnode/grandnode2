using Grand.Data;
using Grand.Infrastructure;
using Grand.Web.Vendor.Interfaces;
using Grand.Web.Vendor.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Vendor.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        if (!DataSettingsManager.DatabaseIsInstalled())
            return;

        services.AddScoped<IProductViewModelService, ProductViewModelService>();
        services.AddScoped<IOrderViewModelService, OrderViewModelService>();
        services.AddScoped<IShipmentViewModelService, ShipmentViewModelService>();
        services.AddScoped<IMerchandiseReturnViewModelService, MerchandiseReturnViewModelService>();
        services.AddScoped<IVendorReviewViewModelService, VendorReviewViewModelService>();
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 102;
    public bool BeforeConfigure => false;
}