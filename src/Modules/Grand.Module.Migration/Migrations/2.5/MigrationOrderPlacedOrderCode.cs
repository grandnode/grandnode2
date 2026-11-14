using Grand.Data;
using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

/// <summary>
///     Shows the order code in the e-mail confirming an order: a guest asks for the order's link with it. Only a body
///     still holding the order number row as the installer seeded it gets the code row after it (the installer is not
///     referenced from here, so both rows are repeated below - a test keeps them equal); a template the operator
///     rewrote is left as it is. Idempotent: a body showing the code already is not touched again.
/// </summary>
public class MigrationOrderPlacedOrderCode : IMigration
{
    private const string TemplateName = "OrderPlaced.CustomerNotification";

    public const string OrderNumberRow =
        "<tr><td style='padding: 0.4em 0;'><strong>Order number:</strong></td><td style='padding: 0.4em 0;'>{{Order.OrderNumber}}</td></tr>";

    public const string OrderCodeRow =
        "<tr><td style='padding: 0.4em 0;'><strong>Order code:</strong></td><td style='padding: 0.4em 0;'>{{Order.OrderCode}}</td></tr>";

    public int Priority => 2;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("6d01ca28-f3cd-4997-b289-3e15bd7976a3");
    public string Name => "Order code in the order confirmation e-mail 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationOrderPlacedOrderCode>>();

        try
        {
            var repository = serviceProvider.GetRequiredService<IRepository<MessageTemplate>>();
            //one per store when the operator made copies for stores
            var templates = await repository.ToListAsync(repository.Table.Where(x => x.Name == TemplateName));
            var updated = false;
            foreach (var template in templates)
            {
                var body = WithOrderCode(template.Body);
                if (body == template.Body)
                    continue;

                await repository.UpdateField(template.Id, x => x.Body, body);
                updated = true;
            }

            //the templates are cached as they were before the upgrade
            var cache = serviceProvider.GetService<ICacheBase>();
            if (updated && cache != null)
                await cache.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - OrderPlacedOrderCode (2.5)");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     The body with the code row after the seeded order number row; the body as it is otherwise
    /// </summary>
    public static string WithOrderCode(string body)
    {
        if (string.IsNullOrEmpty(body) || body.Contains("{{Order.OrderCode}}"))
            return body;

        var index = body.IndexOf(OrderNumberRow, StringComparison.Ordinal);
        if (index < 0)
            return body;

        var end = index + OrderNumberRow.Length;
        return body[..end] + "\n" + OrderCodeRow + body[end..];
    }
}
