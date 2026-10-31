using Grand.Data.LiteDb;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.TypeSearch;
using Grand.Module.Api.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Module.Api.Infrastructure;

public class LiteDbMapperStartup : IStartupApplication
{
    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var dbConfig = new DatabaseConfig();
        configuration.GetSection("Database").Bind(dbConfig);

        //API queries read these models through TableCollection, from the mappers the entities are read from
        if (dbConfig.UseLiteDb)
            LiteDBMapperWarmup.Warm(new TypeSearcher().ClassesOfType<BaseApiEntityModel>());
    }

    public int Priority => 0;
    public bool BeforeConfigure => false;
}
