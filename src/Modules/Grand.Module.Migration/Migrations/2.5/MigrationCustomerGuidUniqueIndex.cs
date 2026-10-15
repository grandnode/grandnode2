using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationCustomerGuidUniqueIndex : IMigration
{
    private const string IndexName = "CustomerGuid_1";

    public int Priority => 0;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("f0c98624-441d-42b6-95ae-429bfb64d266");
    public string Name => "Unique index on Customer.CustomerGuid";

    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<Customer>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationCustomerGuidUniqueIndex>>();

        //LiteDB names indexes by field, so the named drop and recreate below do not apply to it
        //and EnsureIndex never alters an existing index - new installs create the index unique
        if (DataSettingsManager.Instance.LoadSettings().DbProvider != DbProvider.MongoDB)
        {
            logService.LogInformation(
                "The index {IndexName} stays non-unique on this database provider (new installs create it unique)",
                IndexName);
            return true;
        }

        try
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
                return true;
            }

            databaseContext.DeleteIndex(repository, IndexName).GetAwaiter().GetResult();
            databaseContext.CreateIndex(repository,
                    OrderBuilder<Customer>.Create().Ascending(x => x.CustomerGuid), IndexName, true)
                .GetAwaiter().GetResult();

            //when the unique index exists this is a rejected, swallowed no-op; when the unique create failed
            //it restores the lookup index
            databaseContext.CreateIndex(repository,
                    OrderBuilder<Customer>.Create().Ascending(x => x.CustomerGuid), IndexName)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - CustomerGuidUniqueIndex (2.5)");
        }

        return true;
    }
}
