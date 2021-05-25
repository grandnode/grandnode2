using Grand.Domain.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Grand.Infrastructure.Data
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
            RegisterDataLayer(services);
        }

        private void RegisterDataLayer(IServiceCollection serviceCollection)
        {
            var dataSettingsManager = new DataSettingsManager();
            var dataProviderSettings = dataSettingsManager.LoadSettings();
            if (string.IsNullOrEmpty(dataProviderSettings.ConnectionString))
            {
                serviceCollection.AddTransient(c => dataSettingsManager.LoadSettings());
                serviceCollection.AddTransient<BaseDataProviderManager>(c => new MongoDBDataProviderManager(c.GetRequiredService<DataSettings>()));
                serviceCollection.AddTransient<IDataProvider>(x => x.GetRequiredService<BaseDataProviderManager>().LoadDataProvider());
            }
            if (dataProviderSettings != null && dataProviderSettings.IsValid())
            {
                var connectionString = dataProviderSettings.ConnectionString;
                var mongourl = new MongoUrl(connectionString);
                var databaseName = mongourl.DatabaseName;
                serviceCollection.AddScoped(c => new MongoClient(mongourl).GetDatabase(databaseName));

            }

            serviceCollection.AddScoped<IMongoDBContext, MongoDBContext>();
            //MongoDbRepository

            serviceCollection.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        }
    }
}
