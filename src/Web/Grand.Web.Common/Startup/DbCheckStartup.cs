﻿using Grand.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Startup;

public class DbCheckStartup : IStartupApplication
{
    public int Priority => 0;
    public bool BeforeConfigure => false;

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
        if (!DataSettingsManager.DatabaseIsInstalled())
            return;

        var performanceConfig = application.Services.GetRequiredService<PerformanceConfig>();

        if (!performanceConfig.IgnoreDbVersionCheckMiddleware)
            application.UseMiddleware<DbVersionCheckMiddleware>();
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
    }
}