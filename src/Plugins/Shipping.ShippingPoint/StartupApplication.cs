using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shipping.ShippingPoint.Services;

namespace Shipping.ShippingPoint
{
    public class StartupApplication : IStartupApplication
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IShippingRateCalculationProvider, ShippingPointRateProvider>();
            services.AddScoped<IShippingPointService, ShippingPointService>();
        }

        public int Priority => 10;
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {

        }
        public bool BeforeConfigure => false;

    }
}
