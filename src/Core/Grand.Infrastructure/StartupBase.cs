using AutoMapper;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching.RabbitMq;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Extensions;
using Grand.Infrastructure.Mapper;
using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.Roslyn;
using Grand.Infrastructure.TypeConverters;
using Grand.Infrastructure.TypeSearch;
using Grand.Infrastructure.Validators;
using Grand.SharedKernel;
using Grand.SharedKernel.Extensions;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            var dbConfig = services.StartupConfig<DatabaseConfig>(configuration.GetSection("Database"));
            if (!string.IsNullOrEmpty(dbConfig.DbConnectionString))
            {
                DataSettingsManager.LoadDataSettings(new DataSettings {
                    ConnectionString = dbConfig.DbConnectionString,
                    DbProvider = (DbProvider)dbConfig.DbProvider
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
                .Where(PluginExtensions.OnlyInstalledPlugins)
                .Select(mapperConfiguration => (IAutoMapperProfile)Activator.CreateInstance(mapperConfiguration))
                .OrderBy(mapperConfiguration => mapperConfiguration!.Order);

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
                .OrderBy(converter => converter!.Order);

            foreach (var item in instances)
                item.Register();
        }

        /// <summary>
        /// Register type ValidatorConsumer
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="typeSearcher">TypeSearcher</param>
        private static void RegisterValidatorConsumer(IServiceCollection services, ITypeSearcher typeSearcher)
        {
            services.Scan(scan => scan.FromAssemblies(typeSearcher.GetAssemblies())
                .AddClasses(classes => classes.AssignableTo(typeof(IValidatorConsumer<>)))
                .AsImplementedInterfaces().WithScopedLifetime());
        }


        private static T StartupConfig<T>(this IServiceCollection services, IConfiguration configuration)
            where T : class, new()
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

            //Load CTX scripts
            RoslynCompiler.Load(mvcCoreBuilder.PartManager, configuration);
        }

        /// <summary>
        /// Adds services for mediatR
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="typeSearcher"></param>
        private static void AddMediator(this IServiceCollection services, ITypeSearcher typeSearcher)
        {
            var assemblies = typeSearcher.GetAssemblies().ToArray();
            services.AddMediatR(options =>
            {
                options.RegisterServicesFromAssemblies(assemblies);
            });
        }

        /// <summary>
        /// Add Mass Transit rabbitmq message broker
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="typeSearcher"></param>
        private static void AddMassTransitRabbitMq(IServiceCollection services, IConfiguration configuration,
            ITypeSearcher typeSearcher)
        {
            var config = new RabbitConfig();
            configuration.GetSection("Rabbit").Bind(config);

            if (!config.RabbitEnabled) return;
            services.AddMassTransit(x =>
            {
                x.AddConsumers(q => q != typeof(CacheMessageEventConsumer), typeSearcher.GetAssemblies().ToArray());

                if (config.RabbitCachePubSubEnabled)
                {
                    x.AddConsumer<CacheMessageEventConsumer>()
                        .Endpoint(t => t.Name = config.RabbitCacheReceiveEndpoint);
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
        /// <param name="typeSearcher">Type searcher</param>
        private static IMvcCoreBuilder RegisterApplication(IServiceCollection services, IConfiguration configuration, ITypeSearcher typeSearcher)
        {
            //add accessor to HttpContext
            services.AddHttpContextAccessor();
            //add AppConfig configuration parameters
            services.StartupConfig<AppConfig>(configuration.GetSection("Application"));
            var performanceConfig = services.StartupConfig<PerformanceConfig>(configuration.GetSection("Performance"));
            services.StartupConfig<SecurityConfig>(configuration.GetSection("Security"));
            services.StartupConfig<ExtensionsConfig>(configuration.GetSection("Extensions"));
            services.StartupConfig<CacheConfig>(configuration.GetSection("Cache"));
            services.StartupConfig<AccessControlConfig>(configuration.GetSection("AccessControl"));
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
            
            services.AddTransient<FluentValidationFilter>();
            var mvcCoreBuilder = services.AddMvcCore(options =>
            {
                options.Filters.AddService<FluentValidationFilter>();
                var frontConfig = new FrontendAPIConfig();
                configuration.GetSection("FrontendAPI").Bind(frontConfig);
                if (frontConfig.JsonContentType)
                {
                    options.UseJsonBodyModelBinderProviderInsteadOf<DictionaryModelBinderProvider>();
                    options.UseJsonBodyModelBinderProviderInsteadOf<ComplexObjectModelBinderProvider>();
                }
            });

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
            //find startup configurations provided by other assemblies
            var typeSearcher = new TypeSearcher();
            services.AddSingleton<ITypeSearcher>(typeSearcher);

            //register application
            var mvcBuilder = RegisterApplication(services, configuration, typeSearcher);

            //register extensions 
            RegisterExtensions(mvcBuilder, configuration);

            var startupConfigurations = typeSearcher.ClassesOfType<IStartupApplication>();

            //Register startup
            var instancesBefore = startupConfigurations
                .Where(PluginExtensions.OnlyInstalledPlugins)
                .Select(startup => (IStartupApplication)Activator.CreateInstance(startup))
                .Where(startup => startup!.BeforeConfigure)
                .OrderBy(startup => startup.Priority);

            //configure services
            foreach (var instance in instancesBefore)
                instance.ConfigureServices(services, configuration);

            //register mapper configurations
            InitAutoMapper(typeSearcher);

            //Register custom type converters
            RegisterTypeConverter(typeSearcher);

            //Register type validator consumer
            RegisterValidatorConsumer(services, typeSearcher);

            //add mediator
            AddMediator(services, typeSearcher);

            //Add MassTransit
            AddMassTransitRabbitMq(services, configuration, typeSearcher);

            //Register startup
            var instancesAfter = startupConfigurations
                .Where(PluginExtensions.OnlyInstalledPlugins)
                .Select(startup => (IStartupApplication)Activator.CreateInstance(startup))
                .Where(startup => !startup!.BeforeConfigure)
                .OrderBy(startup => startup.Priority);

            //configure services
            foreach (var instance in instancesAfter)
                instance.ConfigureServices(services, configuration);

            //Execute startup interface
            ExecuteStartupBase(typeSearcher);
        }

        /// <summary>
        /// Configure HTTP request pipeline
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        /// <param name="webHostEnvironment">WebHostEnvironment</param>
        public static void ConfigureRequestPipeline(IApplicationBuilder application,
            IWebHostEnvironment webHostEnvironment)
        {
            //find startup configurations provided by other assemblies
            var typeSearcher = new TypeSearcher();
            var startupConfigurations = typeSearcher.ClassesOfType<IStartupApplication>();

            //create and sort instances of startup configurations
            var instances = startupConfigurations
                .Where(PluginExtensions.OnlyInstalledPlugins)
                .Select(startup => (IStartupApplication)Activator.CreateInstance(startup))
                .OrderBy(startup => startup!.Priority);

            //configure request pipeline
            foreach (var instance in instances)
                instance.Configure(application, webHostEnvironment);
        }

        private static void ExecuteStartupBase(ITypeSearcher typeSearcher)
        {
            var startupBaseConfigurations = typeSearcher.ClassesOfType<IStartupBase>();

            //create and sort instances of startup configurations
            var instances = startupBaseConfigurations
                .Select(startup => (IStartupBase)Activator.CreateInstance(startup))
                .OrderBy(startup => startup!.Priority);

            //execute
            foreach (var instance in instances)
                instance.Execute();
        }

        #endregion
    }
}