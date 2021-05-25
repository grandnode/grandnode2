using Grand.Domain.Data;
using Grand.Domain.Data.Mongo;
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
            RegisterDataLayer(services);
        }

        private void RegisterDataLayer(IServiceCollection serviceCollection)
        {
            var dataProviderSettings = DataSettingsManager.LoadSettings();
            if (string.IsNullOrEmpty(dataProviderSettings.ConnectionString))
            {
                serviceCollection.AddTransient(c => dataProviderSettings);
            }
            if (dataProviderSettings != null && dataProviderSettings.IsValid())
            {
                var connectionString = dataProviderSettings.ConnectionString;
                var mongourl = new MongoUrl(connectionString);
                var databaseName = mongourl.DatabaseName;
                serviceCollection.AddScoped(c => new MongoClient(mongourl).GetDatabase(databaseName));

            }

            serviceCollection.AddScoped<IDatabaseContext, MongoDBContext>();

            //MongoDbRepository
            serviceCollection.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

        }
    }
}
