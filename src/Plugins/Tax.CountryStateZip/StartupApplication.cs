using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tax.CountryStateZip.Services;

namespace Tax.CountryStateZip
{
    public class StartupApplication : IStartupApplication
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ITaxRateService, TaxRateService>();
            services.AddScoped<ITaxProvider, CountryStateZipTaxProvider>();
        }

        public int Priority => 20;
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {

        }
        public bool BeforeConfigure => false;
    }
}
