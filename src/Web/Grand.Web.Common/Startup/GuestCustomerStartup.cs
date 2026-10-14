using Grand.Infrastructure;
using Grand.Web.Common.RequestCustomerResolvers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Startup;

/// <summary>
///     Saves a guest only once it returns its cookie or something is written to it
/// </summary>
public class GuestCustomerStartup : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransientGuestCustomers();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    //second pass (BeforeConfigure false), after Grand.Business.Customers (100) registers ICustomerService
    public int Priority => 1100;

    public bool BeforeConfigure => false;
}
