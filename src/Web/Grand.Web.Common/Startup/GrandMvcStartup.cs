﻿using Grand.Web.Common.Infrastructure;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Startup
{
    /// <summary>
    /// Represents object for the configuring MVC on application startup
    /// </summary>
    public class GrandMvcStartup : IStartupApplication
    {
        /// <summary>
        /// Add and configure any of the middleware
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration root of the application</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            //add healthChecks
            services.AddGrandHealthChecks();

            //add miniprofiler
            services.AddGrandMiniProfiler();

            //add WebMarkupMin
            services.AddHtmlMinification(configuration);

            //add ApplicationInsights
            services.AddApplicationInsights(configuration);

            //add and configure MVC feature
            services.AddGrandMvc(configuration);

        }

        /// <summary>
        /// Configure the using of added middleware
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        /// <param name="webHostEnvironment">WebHostEnvironment</param>
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {
            //add MiniProfiler
            application.UseProfiler();

            //endpoint routing
            application.UseGrandEndpoints();

            //save log application started
            application.LogApplicationStarted();
        }

        /// <summary>
        /// Gets order of this startup configuration implementation
        /// </summary>
        public int Priority => 1000;
        public bool BeforeConfigure => true;

    }
}
