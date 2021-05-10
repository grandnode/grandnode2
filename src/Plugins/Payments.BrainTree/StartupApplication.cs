using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Cms.Interfaces;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Payments.BrainTree
{
    public class StartupApplication : IStartupApplication
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IPaymentProvider, BrainTreePaymentProvider>();
            services.AddScoped<IWidgetProvider, BrainTreeWidgetProvider>();
        }

        public int Priority => 10;
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {

        }
        public bool BeforeConfigure => false;

    }

}
