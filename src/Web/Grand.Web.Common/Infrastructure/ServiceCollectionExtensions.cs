using Azure.Identity;
using FluentValidation;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Data;
using Grand.Domain.Configuration;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.TypeSearch;
using Grand.SharedKernel.Extensions;
using Grand.Web.Common.View;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.WebEncoders;
using StackExchange.Redis;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Grand.Web.Common.Infrastructure;

/// <summary>
///     Represents extensions of IServiceCollection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds services required for anti-forgery support
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="config">Security config</param>
    public static void AddAntiForgery(this IServiceCollection services, SecurityConfig config)
    {
        //override cookie name
        services.AddAntiforgery(options =>
        {
            options.Cookie = new CookieBuilder {
                Name = $"{config.CookiePrefix}Antiforgery"
            };
            options.HeaderName = "X-CSRF-TOKEN";
            if (DataSettingsManager.DatabaseIsInstalled())
                //whether to allow the use of anti-forgery cookies from SSL protected page on the other store pages which are not
                options.Cookie.SecurePolicy = config.CookieSecurePolicyAlways
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;
        });
    }

    /// <summary>
    ///     Adds services required for application session state
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="config">Security config</param>
    public static void AddHttpSession(this IServiceCollection services, SecurityConfig config)
    {
        services.AddSession(options =>
        {
            options.Cookie = new CookieBuilder {
                Name = $"{config.CookiePrefix}Session",
                HttpOnly = true
            };
            if (DataSettingsManager.DatabaseIsInstalled())
                options.Cookie.SecurePolicy = config.CookieSecurePolicyAlways
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;
        });
    }

    /// <summary>
    ///     Adds services required for themes support
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    public static void AddThemes(this IServiceCollection services)
    {
        if (!DataSettingsManager.DatabaseIsInstalled())
            return;

        //themes support
        services.Configure<RazorViewEngineOptions>(options =>
        {
            options.ViewLocationExpanders.Add(new ViewLocationExpander());
        });
    }

    /// <summary>
    ///     Adds data protection services
    /// </summary>
    public static void AddGrandDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConfig = new RedisConfig();
        configuration.GetSection("Redis").Bind(redisConfig);

        var azureConfig = new AzureConfig();
        configuration.GetSection("Azure").Bind(azureConfig);

        if (redisConfig.PersistKeysToRedis)
        {
            services.AddDataProtection(opt => opt.ApplicationDiscriminator = "grandnode")
                .PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect(redisConfig.PersistKeysToRedisUrl));
        }
        else if (azureConfig.PersistKeysToAzureKeyVault || azureConfig.PersistKeysToAzureBlobStorage)
        {
            if (azureConfig.PersistKeysToAzureKeyVault)
                services.AddDataProtection()
                    // This blob must already exist before the application is run
                    .PersistKeysToAzureBlobStorage(azureConfig.PersistKeysAzureBlobStorageConnectionString,
                        azureConfig.DataProtectionContainerName, azureConfig.DataProtectionBlobName)
                    .ProtectKeysWithAzureKeyVault(new Uri(azureConfig.KeyIdentifier),
                        new DefaultAzureCredential());
            else
                services.AddDataProtection()
                    .PersistKeysToAzureBlobStorage(azureConfig.PersistKeysAzureBlobStorageConnectionString,
                        azureConfig.DataProtectionContainerName, azureConfig.DataProtectionBlobName);
        }
        else
        {
            var securityConfig = new SecurityConfig();
            configuration.GetSection("Security").Bind(securityConfig);

            var dataProtectionKeysPath = string.IsNullOrEmpty(securityConfig.KeyPersistenceLocation)
                ? CommonPath.DataProtectionKeysPath
                : securityConfig.KeyPersistenceLocation;
            var dataProtectionKeysFolder = new DirectoryInfo(dataProtectionKeysPath);

            //configure the data protection system to persist keys to the specified directory
            services.AddDataProtection().PersistKeysToFileSystem(dataProtectionKeysFolder);
        }
    }

    /// <summary>
    ///     Adds authentication service
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="configuration">Configuration</param>
    public static void AddGrandAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var securityConfig = new SecurityConfig();
        configuration.GetSection("Security").Bind(securityConfig);

        //set default authentication schemes
        var authenticationBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = GrandCookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme;
        });

        //add main cookie authentication
        authenticationBuilder.AddCookie(GrandCookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.Name =
                securityConfig.CookiePrefix + GrandCookieAuthenticationDefaults.AuthenticationScheme;
            options.Cookie.HttpOnly = true;
            options.LoginPath = GrandCookieAuthenticationDefaults.LoginPath;
            options.AccessDeniedPath = GrandCookieAuthenticationDefaults.AccessDeniedPath;

            options.Cookie.SecurePolicy = securityConfig.CookieSecurePolicyAlways
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = securityConfig.CookieSameSite;
        });

        //add external authentication
        authenticationBuilder.AddCookie(GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme, options =>
        {
            options.Cookie.Name = securityConfig.CookiePrefix +
                                  GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme;
            options.Cookie.HttpOnly = true;
            options.LoginPath = GrandCookieAuthenticationDefaults.LoginPath;
            options.AccessDeniedPath = GrandCookieAuthenticationDefaults.AccessDeniedPath;
            options.Cookie.SecurePolicy = securityConfig.CookieSecurePolicyAlways
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = securityConfig.CookieSameSiteExternalAuth;
        });

        //register external authentication plugins now
        var typeSearcher = new TypeSearcher();
        var externalAuthConfigurations = typeSearcher.ClassesOfType<IAuthenticationBuilder>();
        var externalAuthInstances = externalAuthConfigurations
            .Where(PluginExtensions.OnlyInstalledPlugins)
            .Select(x => (IAuthenticationBuilder)Activator.CreateInstance(x))
            .OrderBy(x => x?.Priority);

        //add new Authentication
        foreach (var instance in externalAuthInstances)
            instance?.AddAuthentication(authenticationBuilder, configuration);
    }

    /// <summary>
    ///     Add and configure MVC for the application
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>A builder for configuring MVC services</returns>
    public static IMvcBuilder AddGrandMvc(this IServiceCollection services, IConfiguration configuration)
    {
        //add basic MVC feature
        var mvcBuilder = services.AddControllersWithViews().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });

        //add view localization
        mvcBuilder.AddViewLocalization();

        var securityConfig = new SecurityConfig();
        configuration.GetSection("Security").Bind(securityConfig);

        if (securityConfig.EnableRuntimeCompilation) mvcBuilder.AddRazorRuntimeCompilation();

        if (securityConfig.UseHsts)
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
            });

        if (securityConfig.UseHttpsRedirection)
            services.AddHttpsRedirection(options =>
            {
                options.RedirectStatusCode = securityConfig.HttpsRedirectionRedirect;
                options.HttpsPort = securityConfig.HttpsRedirectionHttpsPort;
            });

        var appConfig = new AppConfig();
        configuration.GetSection("Application").Bind(appConfig);

        //use session-based temp data provider
        if (appConfig.UseSessionStateTempDataProvider) mvcBuilder.AddSessionStateTempDataProvider();

        //Add fluentValidation
        var typeSearcher = new TypeSearcher();
        var assemblies = typeSearcher.GetAssemblies();
        services.AddValidatorsFromAssemblies(assemblies);

        //register controllers as services, it'll allow to override them
        mvcBuilder.AddControllersAsServices();

        return mvcBuilder;
    }

    public static void AddSettings(this IServiceCollection services)
    {
        var typeSearcher = new TypeSearcher();
        var settings = typeSearcher.ClassesOfType<ISettings>();
        var instances = settings.Select(x => (ISettings)Activator.CreateInstance(x));
        foreach (var item in instances)
            services.AddScoped(item!.GetType(), x =>
            {
                var type = item.GetType();
                var storeId = "";
                var settingService = x.GetRequiredService<ISettingService>();
                var store = x.GetRequiredService<IStoreHelper>().StoreHost;
                if (store != null)
                    storeId = store.Id;

                return settingService.LoadSetting(type, storeId);
            });
    }

    public static void AddGrandHealthChecks(this IServiceCollection services)
    {
        var connection = DataSettingsManager.LoadSettings();
        var hcBuilder = services.AddHealthChecks();
        hcBuilder.AddCheck("self", () => HealthCheckResult.Healthy());
        hcBuilder.AddMongoDb(connection.ConnectionString,
            name: "mongodb-check",
            tags: new[] { "mongodb" });
    }

    public static void AddGrandApplicationInsights(this IServiceCollection services, IConfiguration configuration)
    {
        var applicationInsights = new ApplicationInsightsConfig();
        configuration.GetSection("ApplicationInsights").Bind(applicationInsights);
        if (!string.IsNullOrEmpty(applicationInsights.ConnectionString))
        {
            services.AddApplicationInsightsTelemetry();
            services.AddServiceProfiler();
            services.AddLogging(builder =>
            {
                builder.AddApplicationInsights(
                    config =>
                    {
                        config.ConnectionString = applicationInsights.ConnectionString;
                    },
                    options =>
                    {
                        options.IncludeScopes = false;
                        options.TrackExceptionsAsExceptionTelemetry = false;
                    }
                );
            });
        }
    }

    /// <summary>
    ///     Adds services for WebEncoderOptions
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    public static void AddWebEncoder(this IServiceCollection services)
    {
        if (!DataSettingsManager.DatabaseIsInstalled())
            return;

        services.Configure<WebEncoderOptions>(options =>
        {
            options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
        });
    }


    /// <summary>
    ///     Adds services for detection device
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    public static void AddDetectionDevice(this IServiceCollection services)
    {
        services.AddDetection();
    }
}