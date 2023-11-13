using Grand.Domain.Data;
using Grand.Infrastructure;
using Grand.Web.Vendor.Infrastructure;
using Grand.Web.Vendor.Interfaces;
using Grand.Web.Vendor.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Vendor.Startup
{
    public class StartupApplication : IStartupApplication
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            if (!DataSettingsManager.DatabaseIsInstalled())
                return;

            //themes support
            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.ViewLocationExpanders.Add(new VendorViewLocationExpander());
            });
            
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
}
