using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Widgets.FacebookPixel
{
    public class StartupApplication : IStartupApplication
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IWidgetProvider, FacebookPixelProvider>();
            services.AddScoped<IConsentCookie, FacebookPixelConsentCookie>();
        }

        public int Priority => 10;
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {

        }
        public bool BeforeConfigure => false;
    }

}
