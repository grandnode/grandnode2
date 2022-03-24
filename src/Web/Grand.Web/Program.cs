using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider((context, options) =>
{
    options.ValidateScopes = false;
    options.ValidateOnBuild = false;
});

//use serilog
builder.Host.UseSerilog();

//add configuration
builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
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

});

//create logger
Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();

//add services
Grand.Infrastructure.StartupBase.ConfigureServices(builder.Services, builder.Configuration);

//Allow non ASCII chars in headers
var config = new AppConfig();
builder.Configuration.GetSection("Application").Bind(config);
if (config.AllowNonAsciiCharInHeaders)
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ResponseHeaderEncodingSelector = (_) => Encoding.UTF8;
    });
}

//register task
builder.Services.RegisterTasks();

//build app
var app = builder.Build();

//request pipeline
Grand.Infrastructure.StartupBase.ConfigureRequestPipeline(app, builder.Environment);

//run app
app.Run();
