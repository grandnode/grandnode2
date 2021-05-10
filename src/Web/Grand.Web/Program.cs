using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;

namespace Grand.Web
{
    public class Program
    {

        public static void Main(string[] args)
        {
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    config.AddJsonFile("App_Data/appsettings.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                    if (args != null)
                    {
                        config.AddCommandLine(args);
                        var settings = config.Build();
                        var appsettings = settings["appsettings"];
                        var param = settings["Directory"];
                        if (!string.IsNullOrEmpty(appsettings) && !string.IsNullOrEmpty(param))
                            config.AddJsonFile($"App_Data/{param}/appsettings.json", optional: false, reloadOnChange: true);
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.CaptureStartupErrors(true);
                    webBuilder.UseStaticWebAssets();
                    webBuilder.UseSetting(WebHostDefaults.PreventHostingStartupKey, "true");
                    webBuilder.UseStartup<Startup>();
                })
                .UseDefaultServiceProvider((context, options) =>
                {
                    options.ValidateScopes = false;
                    options.ValidateOnBuild = false;
                });
    }
}