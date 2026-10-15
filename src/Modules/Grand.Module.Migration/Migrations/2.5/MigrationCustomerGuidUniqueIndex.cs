using Grand.Data;
using Grand.Data.Mongo;
using Grand.Domain.Customers;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationCustomerGuidUniqueIndex : IMigration
{
    private const string IndexName = "CustomerGuid_1";

    //LiteDB names an index after its field and ignores the given name
    private const string LiteDbIndexName = "CustomerGuid";

    public int Priority => 0;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("f0c98624-441d-42b6-95ae-429bfb64d266");
    public string Name => "Unique index on Customer.CustomerGuid";

    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<Customer>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationCustomerGuidUniqueIndex>>();

        try
        {
            MakeUnique(repository, databaseContext, logService);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - CustomerGuidUniqueIndex (2.5)");
        }

        return true;
    }

    /// <summary>
    ///     Recreates the CustomerGuid index as unique. Returns false when it stays non-unique
    /// </summary>
    public static bool MakeUnique(IRepository<Customer> repository, IDatabaseContext databaseContext, ILogger logService)
    {
        var duplicates = repository.Table
            .GroupBy(x => x.CustomerGuid)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .Take(20)
            .ToList();

        //duplicates must not stop the upgrade - keep the non-unique index for the lookups and report them
        if (duplicates.Count != 0)
        {
            logService.LogError(
                "The index {IndexName} stays non-unique - these customer GUIDs are used more than once: {Guids}. Remove the duplicates and recreate the index as unique",
                IndexName, string.Join(", ", duplicates));
            return false;
        }

        var mongoRepository = repository as MongoRepository<Customer>;
        var keys = OrderBuilder<Customer>.Create().Ascending(x => x.CustomerGuid);

        databaseContext.DeleteIndex(repository, mongoRepository != null ? IndexName : LiteDbIndexName)
            .GetAwaiter().GetResult();
        databaseContext.CreateIndex(repository, keys, IndexName, true).GetAwaiter().GetResult();

        //when the unique index exists this is a rejected, swallowed no-op; when the unique create failed
        //it restores the lookup index
        databaseContext.CreateIndex(repository, keys, IndexName).GetAwaiter().GetResult();

        //CreateIndex swallows its errors: a duplicate saved by a running instance after the check above
        //would leave the index non-unique without a trace
        if (mongoRepository != null && !IsUnique(mongoRepository))
        {
            logService.LogError(
                "The index {IndexName} could not be created unique and stays non-unique. Remove duplicate customer GUIDs and recreate the index as unique",
                IndexName);
            return false;
        }

        return true;
    }

    private static bool IsUnique(MongoRepository<Customer> repository)
    {
        return repository.Collection.Indexes.List().ToList()
            .Any(x => x["name"] == IndexName && x.Contains("unique") && x["unique"].ToBoolean());
    }
}
