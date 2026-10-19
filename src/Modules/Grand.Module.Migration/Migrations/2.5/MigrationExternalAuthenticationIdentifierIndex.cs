using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationExternalAuthenticationIdentifierIndex : IMigration
{
    private const string IndexName = "ProviderSystemName_ExternalIdentifier_StoreId";

    //created by development builds of this migration, before identity links became per store
    private const string PreviousIndexName = "ProviderSystemName_ExternalIdentifier";

    public int Priority => 1;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("5d33a2b4-84d9-467b-8067-3b2fc2336225");
    public string Name => "Unique index on ExternalAuthentication provider + external identifier + store";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<ExternalAuthentication>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationExternalAuthenticationIdentifierIndex>>();

        try
        {
            var duplicates = (await repository.ToListAsync(repository.Table
                    .Select(x => new { x.ProviderSystemName, x.ExternalIdentifier, x.StoreId })))
                //the same key as the index: a missing store and an empty one are different values
                .GroupBy(x => (x.ProviderSystemName, x.ExternalIdentifier, x.StoreId))
                .Where(x => x.Count() > 1)
                .Select(x => $"{x.Key.ProviderSystemName}/{x.Key.ExternalIdentifier}/{x.Key.StoreId}")
                .ToList();

            //duplicates left by older external logins must not stop the upgrade - keep a non-unique index for the
            //lookups and report them
            //LiteDB indexes every field of a composite index on its own, so a unique flag would make each field unique
            var isMongo = DataSettingsManager.Instance.LoadSettings().DbProvider == DbProvider.MongoDB;
            var unique = duplicates.Count == 0 && isMongo;
            if (duplicates.Count != 0)
                logService.LogError(
                    "The index {IndexName} was created as non-unique - these external identities are linked more than once: {Identities}. Remove the duplicates and recreate the index as unique",
                    IndexName, string.Join(", ", duplicates));

            //the two-field unique index would forbid a second link of the subject in another store
            await databaseContext.DeleteIndex(repository, PreviousIndexName);
            await databaseContext.CreateIndex(repository,
                OrderBuilder<ExternalAuthentication>.Create().Ascending(x => x.ProviderSystemName)
                    .Ascending(x => x.ExternalIdentifier).Ascending(x => x.StoreId), IndexName, unique);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - ExternalAuthenticationIdentifierIndex (2.5)");
            return false;
        }

        return true;
    }
}
