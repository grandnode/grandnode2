using Grand.Business.Authentication.Interfaces;
using Grand.Business.Authentication.Utilities;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Configuration;
using Grand.Domain.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.TypeSearchers;
using Grand.SharedKernel.Extensions;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Routing;
using Grand.Web.Common.Themes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.WebEncoders;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using WebMarkupMin.AspNet.Common.Compressors;
using WebMarkupMin.AspNet.Common.UrlMatchers;
using WebMarkupMin.AspNetCore5;
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
        public static void AddAntiForgery(this IServiceCollection services, AppConfig config)
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
        public static void AddHttpSession(this IServiceCollection services, AppConfig config)
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
        /// <param name="services">Collection of service descriptors</param>
        public static void AddGrandDataProtection(this IServiceCollection services, AppConfig config)
        {
            if (config.PersistKeysToRedis)
            {
                services.AddDataProtection(opt => opt.ApplicationDiscriminator = "grandnode")
                    .PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect(config.PersistKeysToRedisUrl));
            }
            else
            {
                var dataProtectionKeysPath = CommonPath.DataProtectionKeysPath;
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
            var config = new AppConfig();
            configuration.GetSection("Application").Bind(config);

            //set default authentication schemes
            var authenticationBuilder = services.AddAuthentication(options =>
            {
                options.DefaultScheme = GrandCookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme;
            });

            //add main cookie authentication
            authenticationBuilder.AddCookie(GrandCookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = config.CookiePrefix + GrandCookieAuthenticationDefaults.AuthenticationScheme;
                options.Cookie.HttpOnly = true;
                options.LoginPath = GrandCookieAuthenticationDefaults.LoginPath;
                options.AccessDeniedPath = GrandCookieAuthenticationDefaults.AccessDeniedPath;

                options.Cookie.SecurePolicy = config.CookieSecurePolicyAlways ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
            });

            //add external authentication
            authenticationBuilder.AddCookie(GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme, options =>
            {
                options.Cookie.Name = config.CookiePrefix + GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme;
                options.Cookie.HttpOnly = true;
                options.LoginPath = GrandCookieAuthenticationDefaults.LoginPath;
                options.AccessDeniedPath = GrandCookieAuthenticationDefaults.AccessDeniedPath;
                options.Cookie.SecurePolicy = config.CookieSecurePolicyAlways ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
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
            var mvcBuilder = services.AddMvc(options =>
            {
                //for API - ignore for PWA
                options.Conventions.Add(new ApiExplorerIgnores());
            });

            //add view localization
            mvcBuilder.AddViewLocalization();
            //add razor runtime compilation
            mvcBuilder.AddRazorRuntimeCompilation();

            var config = new AppConfig();
            configuration.GetSection("Application").Bind(config);

            if (config.UseHsts)
            {
                services.AddHsts(options =>
                {
                    options.Preload = true;
                    options.IncludeSubDomains = true;
                });
            }

            if (config.UseHttpsRedirection)
            {
                services.AddHttpsRedirection(options =>
                {
                    options.RedirectStatusCode = config.HttpsRedirectionRedirect;
                    options.HttpsPort = config.HttpsRedirectionHttpsPort;
                });
            }
            //use session-based temp data provider
            if (config.UseSessionStateTempDataProvider)
            {
                mvcBuilder.AddSessionStateTempDataProvider();
            }

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
                    !request.HttpContext.RequestServices.GetRequiredService<AppConfig>().DisplayMiniProfilerInPublicStore ||
                    request.HttpContext.RequestServices.GetRequiredService<IPermissionService>().Authorize(StandardPermission.AccessAdminPanel).Result;
            });
        }

        /// <summary>
        /// Register custom RedirectResultExecutor
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        public static void AddGrandRedirectResultExecutor(this IServiceCollection services)
        {
            //we use custom redirect executor as a workaround to allow using non-ASCII characters in redirect URLs
            services.AddSingleton<IActionResultExecutor<RedirectResult>, GrandRedirectResultExecutor>();
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
                    var store = x.GetRequiredService<IStoreHelper>().HostStore;
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
            var config = new AppConfig();
            configuration.GetSection("Application").Bind(config);
            if (config.UseHtmlMinification)
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
            if (config.HtmlMinificationErrors)
                services.AddSingleton<IWmmLogger, WmmThrowExceptionLogger>();
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


        /// <summary>
        /// Add Progressive Web App
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        public static void AddPWA(this IServiceCollection services, IConfiguration configuration)
        {
            if (!DataSettingsManager.DatabaseIsInstalled())
                return;

            var config = new AppConfig();
            configuration.GetSection("Application").Bind(config);
            if (config.EnableProgressiveWebApp)
            {
                var options = new WebEssentials.AspNetCore.Pwa.PwaOptions {
                    Strategy = (WebEssentials.AspNetCore.Pwa.ServiceWorkerStrategy)config.ServiceWorkerStrategy,
                    RoutesToIgnore = "/admin/*"
                };
                services.AddProgressiveWebApp(options);
            }
        }
    }
}