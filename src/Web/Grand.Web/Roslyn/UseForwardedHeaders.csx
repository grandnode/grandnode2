#! "net5.0"
#r "Grand.Infrastructure"
#r "Grand.Web"

using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System;

/* Sample code to Forwarded Headers Middleware */
/* More info you can find here: https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-5.0 */

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
    }
    public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
    {
        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardLimit = 2,
            ForwardedForHeaderName = "X-Forwarded-For-My-Custom-Header-Name"
        };
        forwardedHeadersOptions.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));

        application.UseForwardedHeaders(forwardedHeadersOptions);

    }
    public int Priority => -19;
    public bool BeforeConfigure => false;
}
