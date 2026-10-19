using Grand.Data;
using Grand.Data.Mongo;
using Grand.Domain.Customers;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<Customer>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationCustomerGuidUniqueIndex>>();

        try
        {
            await MakeUnique(repository, databaseContext, logService);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - CustomerGuidUniqueIndex (2.5)");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Recreates the CustomerGuid index as unique. Returns false when duplicates keep it non-unique;
    ///     throws, with the non-unique index restored, when the database rejects the unique one
    /// </summary>
    public static async Task<bool> MakeUnique(IRepository<Customer> repository, IDatabaseContext databaseContext, ILogger logService)
    {
        var duplicates = await repository.ToListAsync(repository.Table
            .GroupBy(x => x.CustomerGuid)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .Take(20));

        //duplicates must not stop the upgrade - keep the non-unique index for the lookups and report them
        if (duplicates.Count != 0)
        {
            logService.LogError(
                "The index {IndexName} stays non-unique - these customer GUIDs are used more than once: {Guids}. Remove the duplicates and recreate the index as unique",
                IndexName, string.Join(", ", duplicates));
            return false;
        }

        var keys = OrderBuilder<Customer>.Create().Ascending(x => x.CustomerGuid);

        await databaseContext.DeleteIndex(repository, repository is MongoRepository<Customer> ? IndexName : LiteDbIndexName);
        try
        {
            await databaseContext.CreateIndex(repository, keys, IndexName, true);
        }
        catch
        {
            //e.g. a duplicate saved by a running instance after the check above - keep the lookup index
            //until the retry
            await databaseContext.CreateIndex(repository, keys, IndexName);
            throw;
        }

        return true;
    }
}
