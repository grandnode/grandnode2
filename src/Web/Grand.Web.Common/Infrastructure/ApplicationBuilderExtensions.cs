using Grand.Data;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Endpoints;
using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.TypeSearch;
using Grand.SharedKernel.Extensions;
using Grand.Web.Common.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Grand.Web.Common.Infrastructure;

/// <summary>
///     Represents extensions of IApplicationBuilder
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    ///     Add exception handling
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public static void UseGrandExceptionHandler(this IApplicationBuilder application)
    {
        var serviceProvider = application.ApplicationServices;
        var appConfig = serviceProvider.GetRequiredService<AppConfig>();
        var hostingEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
        var useDetailedExceptionPage = appConfig.DisplayFullErrorStack || hostingEnvironment.IsDevelopment();
        if (useDetailedExceptionPage)
            //get detailed exceptions for developing and testing purposes
            application.UseDeveloperExceptionPage();
        else
            //or use special exception handler
            application.UseExceptionHandler("/errorpage.htm");

        //log errors
        application.UseExceptionHandler(handler =>
        {
            handler.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                if (exception == null)
                    return;

                string authHeader = context.Request.Headers["Authorization"];
                var apiRequest = authHeader != null && authHeader.Split(' ')[0] == "Bearer";
                if (apiRequest)
                {
                    await context.Response.WriteAsync(exception.Message);
                    return;
                }

                try
                {
                    //check whether database is installed
                    if (DataSettingsManager.DatabaseIsInstalled())
                    {
                        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                            .CreateLogger("UseExceptionHandler");
                        //log error
                        logger.LogError(exception, exception.Message);
                    }
                }
                finally
                {
                    //rethrow the exception to show the error page
                    throw exception;
                }
            });
        });
    }

    /// <summary>
    ///     Adds a special handler that checks for responses with the 404 status code that do not have a body
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public static void UsePageNotFound(this IApplicationBuilder application)
    {
        application.UseStatusCodePages(async context =>
        {
            //handle 404 Not Found
            if (context.HttpContext.Response.StatusCode == 404)
            {
                string authHeader = context.HttpContext.Request.Headers[HeaderNames.Authorization];
                var apiRequest = authHeader != null &&
                                 authHeader.Split(' ')[0] == JwtBearerDefaults.AuthenticationScheme;

                var contentTypeProvider = new FileExtensionContentTypeProvider();
                var staticResource = contentTypeProvider.TryGetContentType(context.HttpContext.Request.Path, out _);

                if (!apiRequest && !staticResource)
                {
                    const string location = "/page-not-found";
                    context.HttpContext.Response.Redirect(context.HttpContext.Request.PathBase + location);
                }
            }

            await Task.CompletedTask;
        });
    }

    /// <summary>
    ///     Adds a special handler that checks for responses with the 400 status code (bad request)
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public static void UseBadRequestResult(this IApplicationBuilder application)
    {
        application.UseStatusCodePages(context =>
        {
            //handle 400 (Bad request)
            if (context.HttpContext.Response.StatusCode != StatusCodes.Status400BadRequest)
                return Task.CompletedTask;

            string authHeader = context.HttpContext.Request.Headers[HeaderNames.Authorization];
            var apiRequest = authHeader != null && authHeader.Split(' ')[0] == JwtBearerDefaults.AuthenticationScheme;

            if (apiRequest) return Task.CompletedTask;
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("UseBadRequestResult");
            logger.LogError("Error 400. Bad request");
            return Task.CompletedTask;
        });
    }

    /// <summary>
    ///     Configure authentication
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public static void UseGrandAuthentication(this IApplicationBuilder application)
    {
        application.UseAuthentication();
        application.UseAuthorization();
    }

    /// <summary>
    ///     Configure MVC endpoint
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public static void UseGrandEndpoints(this IApplicationBuilder application)
    {
        application.UseEndpoints(endpoints =>
        {
            var typeSearcher = endpoints.ServiceProvider.GetRequiredService<ITypeSearcher>();
            var endpointProviders = typeSearcher.ClassesOfType<IEndpointProvider>();
            var instances = endpointProviders
                .Where(PluginExtensions.OnlyInstalledPlugins)
                .Select(endpointProvider => (IEndpointProvider)Activator.CreateInstance(endpointProvider))
                .OrderByDescending(endpointProvider => endpointProvider!.Priority);

            foreach (var endpointProvider in instances)
                endpointProvider.RegisterEndpoint(endpoints);
        });
    }

    /// <summary>
    ///     Configure MVC endpoint
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public static void UseGrandDetection(this IApplicationBuilder application)
    {
        application.UseDetection();
    }

    /// <summary>
    ///     Configure static file serving
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    /// <param name="appConfig">AppConfig</param>
    public static void UseGrandStaticFiles(this IApplicationBuilder application, AppConfig appConfig)
    {
        //static files
        application.UseStaticFiles(new StaticFileOptions {
            OnPrepareResponse = ctx =>
            {
                if (!string.IsNullOrEmpty(appConfig.StaticFilesCacheControl))
                    ctx.Context.Response.Headers.Append(HeaderNames.CacheControl, appConfig.StaticFilesCacheControl);
            }
        });

        //plugins
        if (Directory.Exists(CommonPath.PluginsPath))
            application.UseStaticFiles(new StaticFileOptions {
                FileProvider = new PhysicalFileProvider(CommonPath.PluginsPath),
                RequestPath = new PathString("/Plugins"),
                OnPrepareResponse = ctx =>
                {
                    if (!string.IsNullOrEmpty(appConfig.StaticFilesCacheControl))
                        ctx.Context.Response.Headers.Append(HeaderNames.CacheControl,
                            appConfig.StaticFilesCacheControl);
                }
            });
    }

    /// <summary>
    ///     Configure UseForwardedHeaders
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public static void UseGrandForwardedHeaders(this IApplicationBuilder application)
    {
        application.UseForwardedHeaders(new ForwardedHeadersOptions {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });
    }

    /// <summary>
    ///     Configure Health checks
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public static void UseGrandHealthChecks(this IApplicationBuilder application)
    {
        application.UseHealthChecks("/health/live");
    }

    /// <summary>
    ///     Configures the default security headers for your application.
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public static void UseDefaultSecurityHeaders(this IApplicationBuilder application)
    {
        var policyCollection = new HeaderPolicyCollection()
            .AddXssProtectionBlock()
            .AddFrameOptionsDeny()
            .AddContentTypeOptionsNoSniff()
            .AddStrictTransportSecurityMaxAgeIncludeSubDomains() // max-age = one year in seconds
            .AddReferrerPolicyStrictOriginWhenCrossOrigin()
            .AddContentSecurityPolicy(builder =>
            {
                builder.AddUpgradeInsecureRequests();
                builder.AddDefaultSrc().Self();
                builder.AddConnectSrc().From("*");
                builder.AddFontSrc().From("*");
                builder.AddFrameAncestors().From("*");
                builder.AddFrameSrc().From("*");
                builder.AddMediaSrc().From("*");
                builder.AddImgSrc().From("*").Data();
                builder.AddObjectSrc().From("*");
                builder.AddScriptSrc().From("*").UnsafeInline().UnsafeEval();
                builder.AddStyleSrc().From("*").UnsafeEval().UnsafeInline();
            })
            .AddPermissionsPolicy(builder =>
            {
                builder.AddAutoplay().Self();
                builder.AddCamera().Self();
                builder.AddEncryptedMedia().Self();
                builder.AddFullscreen().All();
                builder.AddGeolocation().Self();
                builder.AddGyroscope().None();
                builder.AddMagnetometer().None();
                builder.AddMicrophone().Self();
                builder.AddMidi().None();
                builder.AddPayment().Self();
                builder.AddPictureInPicture().None();
                builder.AddSyncXHR().None();
                builder.AddUsb().None();
            })
            .RemoveServerHeader();

        application.UseSecurityHeaders(policyCollection);
    }

    /// <summary>
    ///     Configure middleware checking whether database is installed
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public static void UseInstallUrl(this IApplicationBuilder application)
    {
        application.UseMiddleware<InstallUrlMiddleware>();
    }

    /// <summary>
    ///     Configures whether use or not the Header X-Powered-By and its value.
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public static void UsePoweredBy(this IApplicationBuilder application)
    {
        application.UseMiddleware<PoweredByMiddleware>();
    }
}