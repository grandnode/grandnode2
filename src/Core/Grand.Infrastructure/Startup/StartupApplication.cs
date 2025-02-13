using Grand.Data;
using Grand.Data.LiteDb;
using Grand.Data.Mongo;
using Grand.Infrastructure.Configuration;
using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Grand.Infrastructure.Startup;

public class StartupApplication : IStartupApplication
{
    public int Priority => 0;

    public bool BeforeConfigure => false;


    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        RegisterDataLayer(services, configuration);
    }

    private void RegisterDataLayer(IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddSingleton<IAuditInfoProvider, AuditInfoProvider>();

        var dbConfig = new DatabaseConfig();
        configuration.GetSection("Database").Bind(dbConfig);

        var dataProviderSettings = DataSettingsManager.Instance.LoadSettings();
        if (string.IsNullOrEmpty(dataProviderSettings.ConnectionString))
        {
            serviceCollection.AddTransient(_ => dataProviderSettings);

            if (dbConfig.UseLiteDb)
                serviceCollection.AddSingleton(_ => new LiteDatabase(dbConfig.LiteDbConnectionString));
        }

        if (dataProviderSettings.IsValid())
        {
            if (dataProviderSettings.DbProvider != DbProvider.LiteDB)
            {
                var connectionString = dataProviderSettings.ConnectionString;
                var mongoUrl = new MongoUrl(connectionString);
                var databaseName = mongoUrl.DatabaseName;
                var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
                serviceCollection.AddScoped(_ => new MongoClient(clientSettings).GetDatabase(databaseName));
            }
            else
            {
                if (dbConfig.Singleton)
                    serviceCollection.AddSingleton(_ => new LiteDatabase(dataProviderSettings.ConnectionString)
                        { UtcDate = true });
                else
                    serviceCollection.AddScoped(_ => new LiteDatabase(dataProviderSettings.ConnectionString)
                        { UtcDate = true });
            }
        }

        if (!dbConfig.UseLiteDb)
        {
            //database context
            serviceCollection.AddScoped<IDatabaseContext, MongoDBContext>();
            //store files context - grid fs
            serviceCollection.AddScoped<IStoreFilesContext, MongoStoreFilesContext>();
            //Mongo Repository
            serviceCollection.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));
        }
        else
        {
            //database context
            serviceCollection.AddScoped<IDatabaseContext, LiteDBContext>();
            //store files context - grid fs
            serviceCollection.AddScoped<IStoreFilesContext, LiteDBStoreFilesContext>();
            //Mongo Repository
            serviceCollection.AddScoped(typeof(IRepository<>), typeof(LiteDBRepository<>));
        }
    }
}