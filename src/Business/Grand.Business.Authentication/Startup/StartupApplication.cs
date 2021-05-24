using Grand.Business.Authentication.Interfaces;
using Grand.Business.Authentication.Services;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.Authentication.Startup
{
    public class StartupApplication : IStartupApplication
    {

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IGrandAuthenticationService, CookieAuthenticationService>();
            services.AddScoped<IApiAuthenticationService, ApiAuthenticationService>();
            services.AddScoped<IJwtBearerAuthenticationService, JwtBearerAuthenticationService>();
            services.AddScoped<ITwoFactorAuthenticationService, TwoFactorAuthenticationService>();
            services.AddScoped<IExternalAuthenticationService, ExternalAuthenticationService>();
            services.AddScoped<IJwtBearerCustomerAuthenticationService, JwtBearerCustomerAuthenticationService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        }
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {

        }
        public int Priority => 100;
        public bool BeforeConfigure => false;

    }
}
