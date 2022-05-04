using Azure.Identity;
using FluentValidation.AspNetCore;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Configuration;
using Grand.Domain.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.TypeSearchers;
using Grand.SharedKernel.Extensions;
using Grand.Web.Common.Themes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.WebEncoders;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;
using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using WebMarkupMin.AspNet.Common.Compressors;
using WebMarkupMin.AspNet.Common.UrlMatchers;
using WebMarkupMin.AspNetCore6;
using WebMarkupMin.NUglify;

using IWmmLogger = WebMarkupMin.Core.Loggers.ILogger;
using WmmThrowExceptionLogger = WebMarkupMin.Core.Loggers.ThrowExceptionLogger;

namespace Grand.Web.Common.Infrastructure
{
    /// <summary>
    /// Represents extensions of IServiceCollection
    /// </summary>
    public static class ServiceCollectionExtensions
    {

        /// <summary>
        /// Adds services required for anti-forgery support
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        public static void AddAntiForgery(this IServiceCollection services, SecurityConfig config)
        {
            //override cookie name
            services.AddAntiforgery(options =>
            {
                options.Cookie = new CookieBuilder() {
                    Name = $"{config.CookiePrefix}Antiforgery"
                };
                if (DataSettingsManager.DatabaseIsInstalled())
                {
                    //whether to allow the use of anti-forgery cookies from SSL protected page on the other store pages which are not
                    options.Cookie.SecurePolicy = config.CookieSecurePolicyAlways ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;

                }
            });
        }

        /// <summary>
        /// Adds services required for application session state
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        public static void AddHttpSession(this IServiceCollection services, SecurityConfig config)
        {
            services.AddSession(options =>
            {
                options.Cookie = new CookieBuilder() {
                    Name = $"{config.CookiePrefix}Session",
                    HttpOnly = true,
                };
                if (DataSettingsManager.DatabaseIsInstalled())
                {
                    options.Cookie.SecurePolicy = config.CookieSecurePolicyAlways ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
                }
            });
        }

        /// <summary>
        /// Adds services required for themes support
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        public static void AddThemes(this IServiceCollection services)
        {
            if (!DataSettingsManager.DatabaseIsInstalled())
                return;

            //themes support
            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.ViewLocationExpanders.Add(new ThemeViewLocationExpander());
            });
        }

        /// <summary>
        /// Adds data protection services
        /// </summary>
        public static void AddGrandDataProtection(this IServiceCollection services, IConfiguration configuration)
        {
            var redisconfig = new RedisConfig();
            configuration.GetSection("Redis").Bind(redisconfig);

            var azureconfig = new AzureConfig();
            configuration.GetSection("Azure").Bind(azureconfig);

            if (redisconfig.PersistKeysToRedis)
            {
                services.AddDataProtection(opt => opt.ApplicationDiscriminator = "grandnode")
                    .PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect(redisconfig.PersistKeysToRedisUrl));
            }
            else if (azureconfig.PersistKeysToAzureKeyVault || azureconfig.PersistKeysToAzureBlobStorage)
            {
                if (azureconfig.PersistKeysToAzureKeyVault)
                    services.AddDataProtection()
                        // This blob must already exist before the application is run
                        .PersistKeysToAzureBlobStorage(azureconfig.PersistKeysAzureBlobStorageConnectionString, azureconfig.DataProtectionContainerName, azureconfig.DataProtectionBlobName)
                        .ProtectKeysWithAzureKeyVault(new Uri(azureconfig.KeyIdentifier),
                        new DefaultAzureCredential());
                else
                {
                    services.AddDataProtection()
                        .PersistKeysToAzureBlobStorage(azureconfig.PersistKeysAzureBlobStorageConnectionString, azureconfig.DataProtectionContainerName, azureconfig.DataProtectionBlobName);
                }

            }
            else
            {
                var securityconfig = new SecurityConfig();
                configuration.GetSection("Security").Bind(securityconfig);

                var dataProtectionKeysPath = string.IsNullOrEmpty(securityconfig.KeyPersistenceLocation) ? CommonPath.DataProtectionKeysPath : securityconfig.KeyPersistenceLocation;
                var dataProtectionKeysFolder = new DirectoryInfo(dataProtectionKeysPath);

                //configure the data protection system to persist keys to the specified directory
                services.AddDataProtection().PersistKeysToFileSystem(dataProtectionKeysFolder);
            }
        }

        /// <summary>
        /// Adds authentication service
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        public static void AddGrandAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var securityconfig = new SecurityConfig();
            configuration.GetSection("Security").Bind(securityconfig);

            //set default authentication schemes
            var authenticationBuilder = services.AddAuthentication(options =>
            {
                options.DefaultScheme = GrandCookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme;
            });

            //add main cookie authentication
            authenticationBuilder.AddCookie(GrandCookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = securityconfig.CookiePrefix + GrandCookieAuthenticationDefaults.AuthenticationScheme;
                options.Cookie.HttpOnly = true;
                options.LoginPath = GrandCookieAuthenticationDefaults.LoginPath;
                options.AccessDeniedPath = GrandCookieAuthenticationDefaults.AccessDeniedPath;

                options.Cookie.SecurePolicy = securityconfig.CookieSecurePolicyAlways ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
            });

            //add external authentication
            authenticationBuilder.AddCookie(GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme, options =>
            {
                options.Cookie.Name = securityconfig.CookiePrefix + GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme;
                options.Cookie.HttpOnly = true;
                options.LoginPath = GrandCookieAuthenticationDefaults.LoginPath;
                options.AccessDeniedPath = GrandCookieAuthenticationDefaults.AccessDeniedPath;
                options.Cookie.SecurePolicy = securityconfig.CookieSecurePolicyAlways ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
            });

            //register external authentication plugins now
            var typeSearcher = new AppTypeSearcher();
            var externalAuthConfigurations = typeSearcher.ClassesOfType<IAuthenticationBuilder>();
            var externalAuthInstances = externalAuthConfigurations
                .Where(x => PluginExtensions.OnlyInstalledPlugins(x))
                .Select(x => (IAuthenticationBuilder)Activator.CreateInstance(x))
                .OrderBy(x => x.Priority);

            //add new Authentication
            foreach (var instance in externalAuthInstances)
                instance.AddAuthentication(authenticationBuilder, configuration);

        }

        /// <summary>
        /// Add and configure MVC for the application
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <returns>A builder for configuring MVC services</returns>
        public static IMvcBuilder AddGrandMvc(this IServiceCollection services, IConfiguration configuration)
        {
            //add basic MVC feature
            var mvcBuilder = services.AddControllersWithViews();

            //add view localization
            mvcBuilder.AddViewLocalization();
            //add razor runtime compilation
            mvcBuilder.AddRazorRuntimeCompilation();

            var securityConfig = new SecurityConfig();
            configuration.GetSection("Security").Bind(securityConfig);

            var appConfig = new AppConfig();
            configuration.GetSection("Application").Bind(appConfig);

            if (securityConfig.UseHsts)
            {
                services.AddHsts(options =>
                {
                    options.Preload = true;
                    options.IncludeSubDomains = true;
                });
            }

            if (securityConfig.UseHttpsRedirection)
            {
                services.AddHttpsRedirection(options =>
                {
                    options.RedirectStatusCode = securityConfig.HttpsRedirectionRedirect;
                    options.HttpsPort = securityConfig.HttpsRedirectionHttpsPort;
                });
            }
            //use session-based temp data provider
            if (appConfig.UseSessionStateTempDataProvider)
            {
                mvcBuilder.AddSessionStateTempDataProvider();
            }

            //Add fluentValidation
            mvcBuilder.AddFluentValidation(configuration =>
            {
                var typeSearcher = new AppTypeSearcher();
                var assemblies = typeSearcher.GetAssemblies();
                configuration.RegisterValidatorsFromAssemblies(assemblies);
                configuration.DisableDataAnnotationsValidation = true;
                //implicit/automatic validation of child properties
                configuration.ImplicitlyValidateChildProperties = true;
            });

            //MVC now serializes JSON with camel case names by default, use this code to avoid it
            mvcBuilder.AddNewtonsoftJson(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver());

            //register controllers as services, it'll allow to override them
            mvcBuilder.AddControllersAsServices();

            return mvcBuilder;
        }

        /// <summary>
        /// Add mini profiler service for the application
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        public static void AddGrandMiniProfiler(this IServiceCollection services)
        {
            //whether database is already installed
            if (!DataSettingsManager.DatabaseIsInstalled())
                return;

            //add MiniProfiler services
            services.AddMiniProfiler(options =>
            {
                options.IgnoredPaths.Add("/api");
                options.IgnoredPaths.Add("/odata");
                options.IgnoredPaths.Add("/health/live");
                options.IgnoredPaths.Add("/.well-known/pki-validation");
                //determine who can access the MiniProfiler results
                options.ResultsAuthorize = request =>
                    !request.HttpContext.RequestServices.GetRequiredService<PerformanceConfig>().DisplayMiniProfilerInPublicStore ||
                    request.HttpContext.RequestServices.GetRequiredService<IPermissionService>().Authorize(StandardPermission.AccessAdminPanel).Result;
            });
        }

        public static void AddSettings(this IServiceCollection services)
        {
            var typeSearcher = new AppTypeSearcher();
            var settings = typeSearcher.ClassesOfType<ISettings>();
            var instances = settings.Select(x => (ISettings)Activator.CreateInstance(x));
            foreach (var item in instances)
            {
                services.AddScoped(item.GetType(), (x) =>
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
        }

        public static void AddGrandHealthChecks(this IServiceCollection services)
        {
            var connection = DataSettingsManager.LoadSettings();
            var hcBuilder = services.AddHealthChecks();
            hcBuilder.AddCheck("self", () => HealthCheckResult.Healthy());
            hcBuilder.AddMongoDb(connection.ConnectionString,
                   name: "mongodb-check",
                   tags: new string[] { "mongodb" });

        }

        public static void AddHtmlMinification(this IServiceCollection services, IConfiguration configuration)
        {
            var performanceConfig = new PerformanceConfig();
            configuration.GetSection("Performance").Bind(performanceConfig);
            if (performanceConfig.UseHtmlMinification)
            {
                // Add WebMarkupMin services
                services.AddWebMarkupMin(options =>
                {
                    options.AllowMinificationInDevelopmentEnvironment = true;
                    options.AllowCompressionInDevelopmentEnvironment = true;
                })
                .AddHtmlMinification(options =>
                {
                    options.MinificationSettings.RemoveOptionalEndTags = false;

                    options.ExcludedPages = new List<IUrlMatcher> {
                    new WildcardUrlMatcher("/swagger/*"),
                    new WildcardUrlMatcher("/admin/*"),
                    new ExactUrlMatcher("/admin"),
                    };
                    options.CssMinifierFactory = new NUglifyCssMinifierFactory();
                    options.JsMinifierFactory = new NUglifyJsMinifierFactory();
                })
                .AddXmlMinification(options =>
                {
                    options.ExcludedPages = new List<IUrlMatcher> {
                    new WildcardUrlMatcher("/swagger/*"),
                    new WildcardUrlMatcher("/admin/*"),
                    new ExactUrlMatcher("/admin"),
                    };
                })
                .AddHttpCompression(options =>
                {
                    options.ExcludedPages = new List<IUrlMatcher> {
                    new WildcardUrlMatcher("/swagger/*"),
                    };
                    options.CompressorFactories = new List<ICompressorFactory>
                        {
                        new BuiltInBrotliCompressorFactory(new BuiltInBrotliCompressionSettings
                        {
                            Level = CompressionLevel.Fastest
                        }),
                        new DeflateCompressorFactory(new DeflateCompressionSettings
                        {
                            Level = CompressionLevel.Fastest
                        }),
                        new GZipCompressorFactory(new GZipCompressionSettings
                        {
                            Level = CompressionLevel.Fastest
                        })
                        };
                });
            }
            if (performanceConfig.HtmlMinificationErrors)
                services.AddSingleton<IWmmLogger, WmmThrowExceptionLogger>();
        }
        public static void AddApplicationInsights(this IServiceCollection services, IConfiguration configuration)
        {
            var applicationInsights = new ApplicationInsightsConfig();
            configuration.GetSection("ApplicationInsights").Bind(applicationInsights);
            if (applicationInsights.Enabled)
            {
                services.AddApplicationInsightsTelemetry();
            }

        }
        /// <summary>
        /// Adds services for WebEncoderOptions
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
        /// Adds services for detection device
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        public static void AddDetectionDevice(this IServiceCollection services)
        {
            services.AddDetection();
        }
    }
}