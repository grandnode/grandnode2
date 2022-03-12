using FluentValidation;
using Grand.Domain.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Message;
using Grand.Infrastructure.Caching.RabbitMq;
using Grand.Infrastructure.Caching.Redis;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.TypeSearchers;
using Grand.Infrastructure.Validators;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Middleware;
using Grand.Web.Common.Page;
using Grand.Web.Common.Routing;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Common.TagHelpers;
using Grand.Web.Common.Themes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using StackExchange.Redis;
using System.Reflection;

namespace Grand.Web.Common.Startup
{
    /// <summary>
    /// Startup application
    /// </summary>
    public class StartupApplication : IStartupApplication
    {
        /// <summary>
        /// Register services and interfaces
        /// </summary>
        /// <param name="ServiceCollection">Service Collection</param>
        /// <param name="typeSearcher">Type finder</param>
        /// <param name="config">Config</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            RegisterCache(services, configuration);

            RegisterContextService(services);

            RegisterValidators(services);

            RegisterFramework(services);
        }

        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {

        }
        public int Priority => 0;
        public bool BeforeConfigure => false;

        private void RegisterCache(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            var config = new RedisConfig();
            configuration.GetSection("Redis").Bind(config);

            serviceCollection.AddSingleton<ICacheBase, MemoryCacheBase>();

            if (config.RedisPubSubEnabled)
            {
                var redis = ConnectionMultiplexer.Connect(config.RedisPubSubConnectionString);
                serviceCollection.AddSingleton<ISubscriber>(c => redis.GetSubscriber());
                serviceCollection.AddSingleton<IMessageBus, RedisMessageBus>();
                serviceCollection.AddSingleton<ICacheBase, RedisMessageCacheManager>();
                return;
            }
            var rabbit = new RabbitConfig();
            configuration.GetSection("Rabbit").Bind(rabbit);
            if (rabbit.RabbitCachePubSubEnabled && rabbit.RabbitEnabled)
            {
                serviceCollection.AddSingleton<ICacheBase, RabbitMqMessageCacheManager>();
            }
        }

        private void RegisterContextService(IServiceCollection serviceCollection)
        {
            //work context
            serviceCollection.AddScoped<IWorkContext, WorkContext>();

            //helper for Settings
            serviceCollection.AddScoped<IStoreHelper, StoreHelper>();
        }


        private void RegisterValidators(IServiceCollection serviceCollection)
        {
            var typeSearcher = new AppTypeSearcher();

            var validators = typeSearcher.ClassesOfType(typeof(IValidator)).ToList();
            foreach (var validator in validators)
            {
                serviceCollection.AddTransient(validator);
            }

            //validator consumers
            var validatorconsumers = typeSearcher.ClassesOfType(typeof(IValidatorConsumer<>)).ToList();
            foreach (var consumer in validatorconsumers)
            {
                var types = consumer.GetTypeInfo().FindInterfaces((type, criteria) =>
                 {
                     var isMatch = type.GetTypeInfo().IsGenericType && ((Type)criteria).IsAssignableFrom(type.GetGenericTypeDefinition());
                     return isMatch;
                 }, typeof(IValidatorConsumer<>));
                foreach (var item in types)
                {
                    serviceCollection.AddScoped(item, consumer);
                }

            }
        }

        private void RegisterFramework(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IPageHeadBuilder, PageHeadBuilder>();

            serviceCollection.AddSingleton<IThemeList, ThemeList>();

            serviceCollection.AddScoped<IThemeProvider, ThemeProvider>();
            serviceCollection.AddScoped<IThemeContext, ThemeContext>();

            serviceCollection.AddScoped<SlugRouteTransformer>();

            serviceCollection.AddScoped<IResourceManager, ResourceManager>();

            if (DataSettingsManager.DatabaseIsInstalled())
                serviceCollection.AddScoped<LocService>();
            else
            {
                var provider = serviceCollection.BuildServiceProvider();
                var _tmp = provider.GetRequiredService<IStringLocalizerFactory>();
                serviceCollection.AddScoped(c => new LocService(_tmp));
            }

            //powered by
            serviceCollection.AddSingleton<IPoweredByMiddlewareOptions, PoweredByMiddlewareOptions>();

            //request reCAPTCHA service
            serviceCollection.AddHttpClient<GoogleReCaptchaValidator>();
        }

    }

}
