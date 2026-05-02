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
using Microsoft.FeatureManagement;
using Microsoft.Net.Http.Headers;

namespace Grand.Web.Common.Infrastructure;

/// <summary>
///     Represents extensions of IApplicationBuilder
/// </summary>
public static class ApplicationBuilderExtensions
{
    // Reused across requests — FileExtensionContentTypeProvider is stateless and thread-safe
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    internal static bool IsApiRequest(HttpRequest request)
    {
        string authHeader = request.Headers[HeaderNames.Authorization];
        return authHeader != null &&
               authHeader.StartsWith(JwtBearerDefaults.AuthenticationScheme + " ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsStaticFileRequest(PathString path)
    {
        return ContentTypeProvider.TryGetContentType(path, out _);
    }

    /// <summary>
    ///     Add exception handling
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public static void UseGrandExceptionHandler(this WebApplication application)
    {
        var appConfig = application.Services.GetRequiredService<AppConfig>();
        var hostingEnvironment = application.Services.GetRequiredService<IWebHostEnvironment>();
        var useDetailedExceptionPage = appConfig.DisplayFullErrorStack || hostingEnvironment.IsDevelopment();
        if (useDetailedExceptionPage)
            //get detailed exceptions for developing and testing purposes
            application.UseDeveloperExceptionPage();
        else
            //use registered IExceptionHandler services (GrandExceptionHandler handles both API and web requests)
            application.UseExceptionHandler();
    }

    /// <summary>
    ///     Adds a special handler that checks for responses with the 404 status code that do not have a body.
    ///     Re-executes the pipeline at /page-not-found (preserving the original 404 status code) while
    ///     skipping the re-execution for API and static-resource requests.
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public static void UsePageNotFound(this WebApplication application)
    {
        // UseStatusCodePagesWithReExecute sets IStatusCodePagesFeature.Enabled = true and re-executes
        // the pipeline at the specified path when a 404 occurs, preserving the original 404 status code.
        application.UseStatusCodePagesWithReExecute("/page-not-found");

        // Disable status code pages for API (Bearer) requests and static resource requests so that
        // those callers receive the original response rather than the HTML not-found page.
        // For all other requests, also restrict re-execution to actual 404 responses so that
        // 400/401/403/405/500 etc. are not mistakenly routed to /page-not-found.
        application.Use(async (context, next) =>
        {
            if (IsApiRequest(context.Request) || IsStaticFileRequest(context.Request.Path))
            {
                var feature = context.Features.Get<IStatusCodePagesFeature>();
                if (feature != null)
                    feature.Enabled = false;
                await next(context);
                return;
            }

            await next(context);

            // Only re-execute for 404 Not Found; all other error codes are handled elsewhere.
            if (context.Response.StatusCode != StatusCodes.Status404NotFound)
            {
                var feature = context.Features.Get<IStatusCodePagesFeature>();
                if (feature != null)
                    feature.Enabled = false;
            }
        });
    }

    /// <summary>
    ///     Adds a special handler that checks for responses with the 400 status code (bad request)
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public static void UseBadRequestResult(this WebApplication application)
    {
        application.UseStatusCodePages(context =>
        {
            //handle 400 (Bad request)
            if (context.HttpContext.Response.StatusCode != StatusCodes.Status400BadRequest)
                return Task.CompletedTask;

            if (IsApiRequest(context.HttpContext.Request)) return Task.CompletedTask;
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
    public static void UseGrandAuthentication(this WebApplication application)
    {
        application.UseAuthentication();
        application.UseAuthorization();
    }

    /// <summary>
    ///     Configure MVC endpoint
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public static void UseGrandEndpoints(this WebApplication application)
    {
        application.UseEndpoints(endpoints =>
        {
            var typeSearcher = application.Services.GetRequiredService<ITypeSearcher>();
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
    ///     Configure static file serving
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    /// <param name="appConfig">AppConfig</param>
    public static void UseGrandStaticFiles(this WebApplication application, AppConfig appConfig)
    {
        //static files
        application.UseStaticFiles(new StaticFileOptions {
            OnPrepareResponse = ctx =>
            {
                if (!string.IsNullOrEmpty(appConfig.StaticFilesCacheControl))
                    ctx.Context.Response.Headers.Append(HeaderNames.CacheControl, appConfig.StaticFilesCacheControl);
            }
        });
        var webHostEnvironment = application.Services.GetRequiredService<IWebHostEnvironment>();
        var pluginsPath = Path.Combine(webHostEnvironment.ContentRootPath, CommonPath.Plugins);

        //plugins
        if (Directory.Exists(pluginsPath))
            application.UseStaticFiles(new StaticFileOptions {
                FileProvider = new PhysicalFileProvider(pluginsPath),
                RequestPath = new PathString($"/{CommonPath.Plugins}"),
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
    public static void UseGrandForwardedHeaders(this WebApplication application)
    {
        application.UseForwardedHeaders(new ForwardedHeadersOptions {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });
    }

    /// <summary>
    ///     Configure Health checks
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public static void UseGrandHealthChecks(this WebApplication application)
    {
        application.UseHealthChecks("/health/live");
    }

    /// <summary>
    ///     Configures the default security headers for your application.
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public static void UseDefaultSecurityHeaders(this WebApplication application)
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
                builder.AddFontSrc().From("*").Data();
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
    public static void UseInstallUrl(this WebApplication application)
    {
        application.UseMiddlewareForFeature<InstallUrlMiddleware>("Grand.Module.Installer");
    }

    /// <summary>
    ///     Configures whether use or not the Header X-Powered-By and its value.
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public static void UsePoweredBy(this WebApplication application)
    {
        application.UseMiddleware<PoweredByMiddleware>();
    }
}