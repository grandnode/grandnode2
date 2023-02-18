﻿using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using StartupBase = Grand.Infrastructure.StartupBase;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider((_, options) =>
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
StartupBase.ConfigureServices(builder.Services, builder.Configuration);

//Allow non ASCII chars in headers
var config = new AppConfig();
builder.Configuration.GetSection("Application").Bind(config);
if (config.AllowNonAsciiCharInHeaders)
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ResponseHeaderEncodingSelector = _ => Encoding.UTF8;
    });
}
if (config.MaxRequestBodySize.HasValue)
{
    builder.WebHost.ConfigureKestrel(host =>
    {
        host.Limits.MaxRequestBodySize = config.MaxRequestBodySize.Value;
    });

    builder.Services.Configure<FormOptions>(opt =>
    {
        opt.MultipartBodyLengthLimit = config.MaxRequestBodySize.Value;
    });

}
//register task
builder.Services.RegisterTasks();

//build app
var app = builder.Build();

//request pipeline
StartupBase.ConfigureRequestPipeline(app, builder.Environment);

//run app
app.Run();
