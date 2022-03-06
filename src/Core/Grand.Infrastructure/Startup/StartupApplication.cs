﻿using Grand.Domain.Data;
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
            var config = new LiteDbConfig();
            configuration.GetSection("LiteDb").Bind(config);
            RegisterDataLayer(services, config);
        }

        private void RegisterDataLayer(IServiceCollection serviceCollection, LiteDbConfig litedbConfig)
        {
            var dataProviderSettings = DataSettingsManager.LoadSettings();
            if (string.IsNullOrEmpty(dataProviderSettings.ConnectionString))
            {
                serviceCollection.AddTransient(c => dataProviderSettings);

                if(litedbConfig.UseLiteDb) 
                    serviceCollection.AddSingleton(c => new LiteDatabase(litedbConfig.LiteDbConnectionString));
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
                    serviceCollection.AddScoped(c => new MongoClient(clientSettings).GetDatabase(databaseName));
                }
                else
                {
                    serviceCollection.AddSingleton(c => new LiteDatabase(dataProviderSettings.ConnectionString) { UtcDate = true }); ;
                }
            }
            if (!litedbConfig.UseLiteDb)
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
