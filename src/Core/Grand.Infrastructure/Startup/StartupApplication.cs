using Grand.Domain.Data;
using Grand.Domain.Data.LiteDb;
using Grand.Domain.Data.Mongo;
using Grand.Infrastructure.Configuration;
using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Grand.Infrastructure.Startup
{
    public class StartupApplication : IStartupApplication
    {
        public int Priority => 0;

        public bool BeforeConfigure => false;


        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {

        }

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            RegisterDataLayer(services, configuration);
        }

        private void RegisterDataLayer(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            var dbConfig = new DatabaseConfig();
            configuration.GetSection("Database").Bind(dbConfig);

            var applicationInsights = new ApplicationInsightsConfig();
            configuration.GetSection("ApplicationInsights").Bind(applicationInsights);

            var dataProviderSettings = DataSettingsManager.LoadSettings();
            if (string.IsNullOrEmpty(dataProviderSettings.ConnectionString))
            {
                serviceCollection.AddTransient(c => dataProviderSettings);

                if (dbConfig.UseLiteDb)
                    serviceCollection.AddSingleton(c => new LiteDatabase(dbConfig.LiteDbConnectionString));
            }
            if (dataProviderSettings != null && dataProviderSettings.IsValid())
            {
                if (dataProviderSettings.DbProvider != DbProvider.LiteDB)
                {
                    var connectionString = dataProviderSettings.ConnectionString;
                    var mongourl = new MongoUrl(connectionString);
                    var databaseName = mongourl.DatabaseName;
                    var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
                    clientSettings.LinqProvider = MongoDB.Driver.Linq.LinqProvider.V3;

                    if (applicationInsights.Enabled)
                        clientSettings.ClusterConfigurator = builder => { builder.Subscribe(new ApplicationInsightsSubscriber(serviceCollection)); };

                    serviceCollection.AddScoped(c => new MongoClient(clientSettings).GetDatabase(databaseName));

                }
                else
                {
                    if (dbConfig.Singleton)
                        serviceCollection.AddSingleton(c => new LiteDatabase(dataProviderSettings.ConnectionString) { UtcDate = true });
                    else
                        serviceCollection.AddScoped(c => new LiteDatabase(dataProviderSettings.ConnectionString) { UtcDate = true });

                }
            }
            if (!dbConfig.UseLiteDb)
            {
                //database context
                serviceCollection.AddScoped<IDatabaseContext, MongoDBContext>();
                //store files context - gridfs
                serviceCollection.AddScoped<IStoreFilesContext, MongoStoreFilesContext>();
                //Mongo Repository
                serviceCollection.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));
            }
            else
            {
                //database context
                serviceCollection.AddScoped<IDatabaseContext, LiteDBContext>();
                //store files context - gridfs
                serviceCollection.AddScoped<IStoreFilesContext, LiteDBStoreFilesContext>();
                //Mongo Repository
                serviceCollection.AddScoped(typeof(IRepository<>), typeof(LiteDBRepository<>));

            }

        }
    }
}
