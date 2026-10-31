using Grand.Data;
using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

/// <summary>
///     Adds the e-mail telling the store it has an order approval to decide. The installer creates the same for new
///     installations (it is not referenced from here, so the body is repeated below - a test keeps them equal).
///     Idempotent: a template that exists (the operator may have created or edited one) is never touched.
/// </summary>
public class MigrationOrderApprovalStaffTemplate : IMigration
{
    private const string TemplateName = "OrderApproval.StaffRequested";

    public int Priority => 2;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("4e7d9a26-1c3b-4f8e-a5d0-6b92c7e3f118");
    public string Name => "Order approval e-mail to the store 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationOrderApprovalStaffTemplate>>();

        try
        {
            var repository = serviceProvider.GetRequiredService<IRepository<MessageTemplate>>();
            var existing = await repository.FirstOrDefaultAsync(repository.Table.Where(x => x.Name == TemplateName));
            if (existing != null)
                return true;

            await repository.InsertAsync(Template(await MigrationOrderApproval.GetEmailAccountId(serviceProvider)));

            //a lookup made before the upgrade may have cached "no such template"
            var cache = serviceProvider.GetService<ICacheBase>();
            if (cache != null)
                await cache.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - OrderApprovalStaffTemplate (2.5)");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     The template, as the installer seeds it: same name, subject and tokens
    /// </summary>
    public static MessageTemplate Template(string emailAccountId)
    {
        return new MessageTemplate {
            Name = TemplateName,
            Subject = "{{Store.Name}}: order #{{OrderApproval.OrderNumber}} needs your decision",
            Body = MigrationOrderApproval.Layout(
                "Order #{{OrderApproval.OrderNumber}} is waiting for a decision.",
                "Order approval needed",
                @"
<p><strong>{{OrderApproval.BuyerName | Escape}}</strong> ({{OrderApproval.BuyerEmail | Escape}}) placed order <strong>#{{OrderApproval.OrderNumber}}</strong> at <strong>{{Store.Name}}</strong>. The order is on hold until someone in the store decides.</p>
{% if OrderApproval.ReasonsHtml != '' %}
<p>Why it needs a decision:</p>
{{OrderApproval.ReasonsHtml}}
{% endif %}"
                + MigrationOrderApproval.Button("{{OrderApproval.PanelUrl}}", "Review the order") + @"
<p>The order contains:</p>"
                + "{{OrderApproval.ItemsHtml}}" + @"
<p style='text-align: right;'><strong>Order total: {{OrderApproval.Total}}</strong></p>"),
            IsActive = true,
            EmailAccountId = emailAccountId
        };
    }
}
