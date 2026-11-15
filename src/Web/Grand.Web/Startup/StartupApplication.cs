using Grand.Infrastructure;
using Grand.Web.Extensions;

namespace Grand.Web.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        //the guest-order grant reads and writes the browser's cookies through IHttpContextAccessor,
        //so it lives in the web layer and is resolved per request
        services.AddScoped<GuestOrderGrant>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 0;
    public bool BeforeConfigure => false;
}
