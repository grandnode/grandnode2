using Grand.Data;
using Grand.Domain.Configuration;
using Grand.Domain.Directory;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Grand.Module.Migration.Migrations._2._4;

/// <summary>
///     Moves the primary store currency out of <see cref="CurrencySettings" /> into its own
///     <see cref="PrimaryCurrencySettings" />, so a single store can override it.
///     Settings fall back to the global value per object rather than per field, so leaving the primary currency on
///     <c>CurrencySettings</c> would mean a store-scoped override also froze that class's system-wide fields - the
///     exchange rate currency, the rate provider and the auto update flag - for the same store.
///     The old <c>PrimaryStoreCurrencyId</c> element is read straight out of the stored metadata, because the
///     property no longer exists on the class. It is left in the document: the global IgnoreExtraElementsConvention
///     skips it on read and it disappears the next time the settings are saved.
/// </summary>
public class MigrationPrimaryCurrencySetting : IMigration
{
    public int Priority => 1;
    public DbVersion Version => new(2, 4);
    public Guid Identity => new("2F91B4C7-6E38-4A05-9D1B-C38E7A426D5F");
    public string Name => "Move the primary store currency into PrimaryCurrencySettings 2.4";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<Setting>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationPrimaryCurrencySetting>>();

        try
        {
            var sourceName = nameof(CurrencySettings).ToLowerInvariant();
            var targetName = nameof(PrimaryCurrencySettings).ToLowerInvariant();

            var currencySettings = repository.Table.Where(x => x.Name == sourceName).ToList();
            var primaryCurrencySettings = repository.Table.Where(x => x.Name == targetName).ToList();

            foreach (var setting in currencySettings)
            {
                var currencyId = ReadPrimaryStoreCurrencyId(setting.Metadata);
                if (string.IsNullOrEmpty(currencyId))
                {
                    logService.LogWarning(
                        "No primary store currency found in the currency settings of store {StoreId} - set it in the admin area, prices cannot be converted without it",
                        string.IsNullOrEmpty(setting.StoreId) ? "(all stores)" : setting.StoreId);
                    continue;
                }

                //never overwrite a value that is already there - the migration must be safe to re-run
                if (primaryCurrencySettings.Any(x => x.StoreId == setting.StoreId))
                    continue;

                repository.Insert(new Setting {
                    Name = targetName,
                    StoreId = setting.StoreId,
                    Metadata = JsonSerializer.Serialize(new PrimaryCurrencySettings { CurrencyId = currencyId })
                });
            }
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - MigrationPrimaryCurrencySetting (2.4)");
        }

        return true;
    }

    private static string ReadPrimaryStoreCurrencyId(string metadata)
    {
        if (string.IsNullOrEmpty(metadata))
            return null;

        using var document = JsonDocument.Parse(metadata);
        return document.RootElement.TryGetProperty("PrimaryStoreCurrencyId", out var currencyId)
            ? currencyId.GetString()
            : null;
    }
}
