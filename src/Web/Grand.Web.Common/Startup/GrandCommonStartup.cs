﻿using Grand.Web.Common.Infrastructure;
using Grand.Web.Common.Routing;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Globalization;

namespace Grand.Web.Common.Startup
{
    /// <summary>
    /// Represents object for the configuring common features and middleware on application startup
    /// </summary>
    public class GrandCommonStartup : IStartupApplication
    {
        /// <summary>
        /// Add and configure any of the middleware
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration root of the application</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var config = new AppConfig();
            configuration.GetSection("Application").Bind(config);

            //add settings
            services.AddSettings();

            //compression
            services.AddResponseCompression();

            //add options feature
            services.AddOptions();

            //add HTTP sesion state feature
            services.AddHttpSession(config);

            //add anti-forgery
            services.AddAntiForgery(config);

            //add localization
            services.AddLocalization();

            //add theme support
            services.AddThemes();

            //add WebEncoderOptions
            services.AddWebEncoder();

            //adddetection device
            services.AddDetectionDevice();

            //add routing
            services.AddRouting(options =>
            {
                options.ConstraintMap["lang"] = typeof(LanguageParameterTransformer);
            });

            //add data protection
            services.AddGrandDataProtection(config);
        }

        /// <summary>
        /// Configure the using of added middleware
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        /// <param name="webHostEnvironment">WebHostEnvironment</param>
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {
            var serviceProvider = application.ApplicationServices;
            var appConfig = serviceProvider.GetRequiredService<AppConfig>();

            //add HealthChecks
            application.UseGrandHealthChecks();

            //default security headers
            if (appConfig.UseDefaultSecurityHeaders)
            {
                application.UseDefaultSecurityHeaders();
            }

            //use hsts
            if (appConfig.UseHsts)
            {
                application.UseHsts();
            }
            //enforce HTTPS in ASP.NET Core
            if (appConfig.UseHttpsRedirection)
            {
                application.UseHttpsRedirection();
            }

            //compression
            if (appConfig.UseResponseCompression)
            {
                //gzip by default
                application.UseResponseCompression();
            }

            //Add webMarkupMin
            if (appConfig.UseHtmlMinification)
            {
                application.UseHtmlMinification();
            }

            //use request localization
            if (appConfig.UseRequestLocalization)
            {
                var supportedCultures = new List<CultureInfo>();
                foreach (var culture in appConfig.SupportedCultures)
                {
                    supportedCultures.Add(new CultureInfo(culture));
                }
                application.UseRequestLocalization(new RequestLocalizationOptions
                {
                    DefaultRequestCulture = new RequestCulture(appConfig.DefaultRequestCulture),
                    SupportedCultures = supportedCultures,
                    SupportedUICultures = supportedCultures
                });
            }
            else
                //use default request localization
                application.UseRequestLocalization();

            //use static files feature
            application.UseGrandStaticFiles(appConfig);

            //check whether database is installed
            if (!appConfig.IgnoreInstallUrlMiddleware)
                application.UseInstallUrl();

            //use HTTP session
            application.UseSession();

            //use powered by
            if (!appConfig.IgnoreUsePoweredByMiddleware)
                application.UsePoweredBy();

            // Write streamlined request completion events, instead of the more verbose ones from the framework.
            // To use the default framework request logging instead, remove this line and set the "Microsoft"
            // level in appsettings.json to "Information".
            if (appConfig.UseSerilogRequestLogging)
                application.UseSerilogRequestLogging();

            //add responsive middleware (for detection)
            application.UseGrandDetection();

            //use routing
            application.UseRouting();

        }

        /// <summary>
        /// Gets order of this startup configuration implementation
        /// </summary>
        public int Priority => 100;
        public bool BeforeConfigure => true;

    }
}
