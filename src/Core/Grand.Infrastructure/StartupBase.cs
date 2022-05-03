using AutoMapper;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching.RabbitMq;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Mapper;
using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.Roslyn;
using Grand.Infrastructure.TypeConverters;
using Grand.Infrastructure.TypeSearchers;
using Grand.SharedKernel;
using Grand.SharedKernel.Extensions;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wkhtmltopdf.NetCore;

namespace Grand.Infrastructure
{
    /// <summary>
    /// Represents startup
    /// </summary>
    public static class StartupBase
    {
        #region Utilities

        /// <summary>
        /// Init Database
        /// </summary>
        private static void InitDatabase(IServiceCollection services, IConfiguration configuration)
        {
            var advancedConfig = services.StartupConfig<AdvancedConfig>(configuration.GetSection("Advanced"));
            if (!string.IsNullOrEmpty(advancedConfig.DbConnectionString))
            {
                DataSettingsManager.LoadDataSettings(new DataSettings() {
                    ConnectionString = advancedConfig.DbConnectionString,
                    DbProvider = (DbProvider)advancedConfig.DbProvider,
                });
            }
        }

        /// <summary>
        /// Register and init AutoMapper
        /// </summary>
        /// <param name="typeSearcher">Type finder</param>
        private static void InitAutoMapper(ITypeSearcher typeSearcher)
        {
            //find mapper configurations provided by other assemblies
            var mapperConfigurations = typeSearcher.ClassesOfType<IAutoMapperProfile>();

            //create and sort instances of mapper configurations
            var instances = mapperConfigurations
                .Where(mapperConfiguration => PluginExtensions.OnlyInstalledPlugins(mapperConfiguration))
                .Select(mapperConfiguration => (IAutoMapperProfile)Activator.CreateInstance(mapperConfiguration))
                .OrderBy(mapperConfiguration => mapperConfiguration.Order);

            //create AutoMapper configuration
            var config = new MapperConfiguration(cfg =>
            {
                foreach (var instance in instances)
                {
                    cfg.AddProfile(instance.GetType());
                }
            });

            //register automapper
            AutoMapperConfig.Init(config);
        }

        /// <summary>
        /// Register type Converters
        /// </summary>
        /// <param name="typeSearcher"></param>
        private static void RegisterTypeConverter(ITypeSearcher typeSearcher)
        {
            //find converters provided by other assemblies
            var converters = typeSearcher.ClassesOfType<ITypeConverter>();

            //create and sort instances of typeConverter 
            var instances = converters
                .Select(converter => (ITypeConverter)Activator.CreateInstance(converter))
                .OrderBy(converter => converter.Order);

            foreach (var item in instances)
                item.Register();
        }

        private static T StartupConfig<T>(this IServiceCollection services, IConfiguration configuration) where T : class, new()
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var config = new T();
            configuration.Bind(config);
            services.AddSingleton(config);
            return config;
        }

        /// <summary>
        /// Register HttpContextAccessor
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        private static void AddHttpContextAccessor(this IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        }

        /// <summary>
        /// Register extensions plugins/scripts
        /// </summary>
        /// <param name="mvcCoreBuilder"></param>
        /// <param name="configuration"></param>
        private static void RegisterExtensions(IMvcCoreBuilder mvcCoreBuilder, IConfiguration configuration)
        {
            //Load plugins
            PluginManager.Load(mvcCoreBuilder, configuration);

            //Load CTX sctipts
            RoslynCompiler.Load(mvcCoreBuilder.PartManager, configuration);
        }

        /// <summary>
        /// Adds services for mediatR
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        private static void AddMediator(this IServiceCollection services, AppTypeSearcher typeSearcher)
        {
            var assemblies = typeSearcher.GetAssemblies().ToArray();
            services.AddMediatR(assemblies);
        }

        /// <summary>
        /// Add Mass Transit rabitMq message broker
        /// </summary>
        /// <param name="services"></param>
        private static void AddMassTransitRabbitMq(IServiceCollection services, IConfiguration configuration, AppTypeSearcher typeSearcher)
        {
            var config = new RabbitConfig();
            configuration.GetSection("Rabbit").Bind(config);

            if (!config.RabbitEnabled) return;
            services.AddMassTransit(x =>
            {
                x.AddConsumers(q => !q.Equals(typeof(CacheMessageEventConsumer)), typeSearcher.GetAssemblies().ToArray());

                // reddits have more priority
                if (config.RabbitCachePubSubEnabled)
                {
                    x.AddConsumer<CacheMessageEventConsumer>().Endpoint(t => t.Name = config.RabbitCacheReceiveEndpoint);
                }

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(config.RabbitHostName, config.RabbitVirtualHost, h =>
                    {
                        h.Password(config.RabbitPassword);
                        h.Username(config.RabbitUsername);
                    });
                    cfg.ConfigureEndpoints(context);
                });
            });
        }

        /// <summary>
        /// Register application 
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration</param>
        private static IMvcCoreBuilder RegisterApplication(IServiceCollection services, IConfiguration configuration)
        {
            //add accessor to HttpContext
            services.AddHttpContextAccessor();
            //add wkhtmltopdf
            services.AddWkhtmltopdf();

            //add AppConfig configuration parameters
            services.StartupConfig<AppConfig>(configuration.GetSection("Application"));
            var performanceConfig = services.StartupConfig<PerformanceConfig>(configuration.GetSection("Performance"));
            var securityConfig = services.StartupConfig<SecurityConfig>(configuration.GetSection("Security"));
            services.StartupConfig<ExtensionsConfig>(configuration.GetSection("Extensions"));
            services.StartupConfig<UrlRewriteConfig>(configuration.GetSection("UrlRewrite"));
            services.StartupConfig<RedisConfig>(configuration.GetSection("Redis"));
            services.StartupConfig<RabbitConfig>(configuration.GetSection("Rabbit"));
            services.StartupConfig<BackendAPIConfig>(configuration.GetSection("BackendAPI"));
            services.StartupConfig<FrontendAPIConfig>(configuration.GetSection("FrontendAPI"));
            services.StartupConfig<DatabaseConfig>(configuration.GetSection("Database"));
            services.StartupConfig<AmazonConfig>(configuration.GetSection("Amazon"));
            services.StartupConfig<AzureConfig>(configuration.GetSection("Azure"));
            services.StartupConfig<ApplicationInsightsConfig>(configuration.GetSection("ApplicationInsights"));

            InitDatabase(services, configuration);

            //set base application path
            var provider = services.BuildServiceProvider();
            var hostingEnvironment = provider.GetRequiredService<IWebHostEnvironment>();
            var param = configuration["Directory"];
            if (!string.IsNullOrEmpty(param))
                CommonPath.Param = param;

            CommonPath.WebHostEnvironment = hostingEnvironment.WebRootPath;
            CommonPath.BaseDirectory = hostingEnvironment.ContentRootPath;
            CommonHelper.CacheTimeMinutes = performanceConfig.DefaultCacheTimeMinutes;
            CommonHelper.CookieAuthExpires = securityConfig.CookieAuthExpires > 0 ? securityConfig.CookieAuthExpires : 24 * 365;

            CommonHelper.IgnoreAcl = performanceConfig.IgnoreAcl;
            CommonHelper.IgnoreStoreLimitations = performanceConfig.IgnoreStoreLimitations;

            var mvcCoreBuilder = services.AddMvcCore();

            return mvcCoreBuilder;
        }

        #endregion


        #region Methods


        /// <summary>
        /// Add and configure services
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration root of the application</param>
        public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            //register application
            var mvcBuilder = RegisterApplication(services, configuration);

            //register extensions 
            RegisterExtensions(mvcBuilder, configuration);

            //find startup configurations provided by other assemblies
            var typeSearcher = new AppTypeSearcher();
            services.AddSingleton<ITypeSearcher>(typeSearcher);

            var startupConfigurations = typeSearcher.ClassesOfType<IStartupApplication>();

            //Register startup
            var instancesBefore = startupConfigurations
                .Where(startup => PluginExtensions.OnlyInstalledPlugins(startup))
                .Select(startup => (IStartupApplication)Activator.CreateInstance(startup))
                .Where(startup => startup.BeforeConfigure)
                .OrderBy(startup => startup.Priority);

            //configure services
            foreach (var instance in instancesBefore)
                instance.ConfigureServices(services, configuration);

            //register mapper configurations
            InitAutoMapper(typeSearcher);

            //Register custom type converters
            RegisterTypeConverter(typeSearcher);

            //add mediator
            AddMediator(services, typeSearcher);

            //Add MassTransit
            AddMassTransitRabbitMq(services, configuration, typeSearcher);

            //Register startup
            var instancesAfter = startupConfigurations
                .Where(startup => PluginExtensions.OnlyInstalledPlugins(startup))
                .Select(startup => (IStartupApplication)Activator.CreateInstance(startup))
                .Where(startup => !startup.BeforeConfigure)
                .OrderBy(startup => startup.Priority);

            //configure services
            foreach (var instance in instancesAfter)
                instance.ConfigureServices(services, configuration);

            //Execute startupbase interface
            ExecuteStartupBase(typeSearcher);
        }

        /// <summary>
        /// Configure HTTP request pipeline
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        /// <param name="webHostEnvironment">WebHostEnvironment</param>
        public static void ConfigureRequestPipeline(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {
            //find startup configurations provided by other assemblies
            var typeSearcher = new AppTypeSearcher();
            var startupConfigurations = typeSearcher.ClassesOfType<IStartupApplication>();

            //create and sort instances of startup configurations
            var instances = startupConfigurations
                .Where(startup => PluginExtensions.OnlyInstalledPlugins(startup))
                .Select(startup => (IStartupApplication)Activator.CreateInstance(startup))
                .OrderBy(startup => startup.Priority);

            //configure request pipeline
            foreach (var instance in instances)
                instance.Configure(application, webHostEnvironment);
        }

        private static void ExecuteStartupBase(AppTypeSearcher typeSearcher)
        {
            var startupBaseConfigurations = typeSearcher.ClassesOfType<IStartupBase>();

            //create and sort instances of startup configurations
            var instances = startupBaseConfigurations
                .Select(startup => (IStartupBase)Activator.CreateInstance(startup))
                .OrderBy(startup => startup.Priority);

            //execute
            foreach (var instance in instances)
                instance.Execute();
        }

        #endregion

    }
}
