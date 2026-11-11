using Grand.Data;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

/// <summary>
///     The items of earlier merchandise returns keep the reason and action only as the name the customer read: the
///     Ids are found from that name, in any of its translations. A name that matches nothing, or more than one reason
///     or action, leaves the Id empty
/// </summary>
public class MigrationMerchandiseReturnReasonIds : IMigration
{
    public int Priority => 1;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("b3e7a2d4-5c91-4f68-8e0b-7d2c4a9f1e36");
    public string Name => "Merchandise return items: reason and action by Id 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var returnRepository = serviceProvider.GetRequiredService<IRepository<MerchandiseReturn>>();
        var reasonRepository = serviceProvider.GetRequiredService<IRepository<MerchandiseReturnReason>>();
        var actionRepository = serviceProvider.GetRequiredService<IRepository<MerchandiseReturnAction>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationMerchandiseReturnReasonIds>>();

        try
        {
            var reasonIds = IdsByName(reasonRepository.Table.ToList().Select(x => (x.Id, x.Name, x.Locales)));
            var actionIds = IdsByName(actionRepository.Table.ToList().Select(x => (x.Id, x.Name, x.Locales)));

            foreach (var merchandiseReturn in returnRepository.Table.ToList())
            {
                var changed = false;
                foreach (var item in merchandiseReturn.MerchandiseReturnItems)
                {
                    if (item.ReasonId == null && reasonIds.TryGetValue(Key(item.ReasonForReturn), out var reasonId))
                    {
                        item.ReasonId = reasonId;
                        changed = true;
                    }

                    if (item.ActionId == null && actionIds.TryGetValue(Key(item.RequestedAction), out var actionId))
                    {
                        item.ActionId = actionId;
                        changed = true;
                    }
                }

                if (changed)
                    await returnRepository.UpdateField(merchandiseReturn.Id, x => x.MerchandiseReturnItems,
                        merchandiseReturn.MerchandiseReturnItems);
            }
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - MerchandiseReturnReasonIds (2.5)");
            return false;
        }

        return true;
    }

    private static string Key(string name)
    {
        return name?.Trim().ToLowerInvariant() ?? "";
    }

    private static Dictionary<string, string> IdsByName(
        IEnumerable<(string Id, string Name, IList<TranslationEntity> Locales)> entities)
    {
        var ids = new Dictionary<string, string>();
        var ambiguous = new HashSet<string>();
        foreach (var (id, name, locales) in entities)
        {
            var names = locales.Where(x => x.LocaleKey == "Name").Select(x => x.LocaleValue).Append(name)
                .Select(Key).Where(x => x.Length > 0).Distinct();
            foreach (var key in names)
                if (ids.TryGetValue(key, out var other) && other != id)
                    ambiguous.Add(key);
                else
                    ids[key] = id;
        }

        foreach (var key in ambiguous)
            ids.Remove(key);
        return ids;
    }
}
