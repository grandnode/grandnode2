using Grand.Business.Customers.Interfaces;
using Grand.Business.Customers.Services;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.Customers.Startup
{
    public class StartupApplication : IStartupApplication
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            RegisterCustomerService(services);

        }
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {

        }
        public int Priority => 100;
        public bool BeforeConfigure => false;

        private void RegisterCustomerService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IVendorService, VendorService>();
            serviceCollection.AddScoped<ICustomerAttributeParser, CustomerAttributeParser>();
            serviceCollection.AddScoped<ICustomerAttributeService, CustomerAttributeService>();
            serviceCollection.AddScoped<ICustomerService, CustomerService>();
            serviceCollection.AddScoped<ICustomerNoteService, CustomerNoteService>();
            serviceCollection.AddScoped<ICustomerHistoryPasswordService, CustomerHistoryPasswordService>();
            serviceCollection.AddScoped<ICustomerManagerService, CustomerManagerService>();
            serviceCollection.AddScoped<ISalesEmployeeService, SalesEmployeeService>();
            serviceCollection.AddScoped<IUserApiService, UserApiService>();
            serviceCollection.AddScoped<IAffiliateService, AffiliateService>();

        }
    }
}
