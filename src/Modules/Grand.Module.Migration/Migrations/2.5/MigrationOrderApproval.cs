using Grand.Data;
using Grand.Domain.Configuration;
using Grand.Domain.Messages;
using Grand.Domain.Orders;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Grand.Module.Migration.Migrations._2._5;

/// <summary>
///     Prepares an existing installation for B2B order approval: the indexes of the OrderApproval collection, the
///     two e-mails it sends, the "Awaiting approval" order status and the default payment methods allowed for such
///     orders. The installer creates the same for new installations (it is not referenced from here,
///     so the template bodies are repeated below - a test keeps their subjects and tokens equal).
///     Idempotent: a template, status or setting that exists (the operator may have created or edited one) is
///     never touched.
/// </summary>
public class MigrationOrderApproval : IMigration
{
    private const string RequestedTemplateName = "OrderApproval.Requested";
    private const string DecidedTemplateName = "OrderApproval.Decided";

    public int Priority => 1;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("b7c3e5a1-48d2-4f60-9b17-3a5e8d02c6f4");
    public string Name => "Order approval indexes, order status, settings and message templates 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var orderApprovalRepository = serviceProvider.GetRequiredService<IRepository<OrderApproval>>();
        var templateRepository = serviceProvider.GetRequiredService<IRepository<MessageTemplate>>();
        var orderStatusRepository = serviceProvider.GetRequiredService<IRepository<OrderStatus>>();
        var settingRepository = serviceProvider.GetRequiredService<IRepository<Setting>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationOrderApproval>>();

        try
        {
            //the same indexes, with the same names, as InstallationService creates
            await databaseContext.CreateIndex(orderApprovalRepository,
                OrderBuilder<OrderApproval>.Create().Ascending(x => x.CompanyOwnerId).Ascending(x => x.Status)
                    .Descending(x => x.CreatedOnUtc), "CompanyOwnerId_Status_CreatedOnUtc");
            await databaseContext.CreateIndex(orderApprovalRepository,
                OrderBuilder<OrderApproval>.Create().Ascending(x => x.CustomerId).Ascending(x => x.StoreId)
                    .Ascending(x => x.Status), "CustomerId_StoreId_Status");
            await databaseContext.CreateIndex(orderApprovalRepository,
                OrderBuilder<OrderApproval>.Create().Ascending(x => x.OrderId), "OrderId");

            var emailAccountId = await GetEmailAccountId(serviceProvider);
            var inserted = false;
            foreach (var template in Templates(emailAccountId))
            {
                var name = template.Name;
                var existing = await templateRepository.FirstOrDefaultAsync(
                    templateRepository.Table.Where(x => x.Name == name));
                if (existing != null) continue;

                await templateRepository.InsertAsync(template);
                inserted = true;
            }

            var statusInserted = await InsertAwaitingApprovalStatus(orderStatusRepository);
            await InsertSettings(settingRepository);

            //a lookup made before the upgrade may have cached "no such template" or the list of statuses without it
            var cache = serviceProvider.GetService<ICacheBase>();
            if (cache != null)
            {
                if (inserted)
                    await cache.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY);
                if (statusInserted)
                    await cache.RemoveByPrefix(CacheKey.ORDER_STATUS_ALL);
            }
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - OrderApproval (2.5)");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Status 5 is reserved for the system: statuses an administrator adds get the highest id plus 10, so it never
    ///     collides with a custom one. A row that has this id already is left as it is.
    /// </summary>
    private static async Task<bool> InsertAwaitingApprovalStatus(IRepository<OrderStatus> repository)
    {
        const int statusId = (int)OrderStatusSystem.AwaitingApproval;
        var existing = await repository.FirstOrDefaultAsync(repository.Table.Where(x => x.StatusId == statusId));
        if (existing != null) return false;

        await repository.InsertAsync(new OrderStatus {
            StatusId = statusId, Name = "Awaiting approval", DisplayOrder = 5, IsSystem = true
        });
        return true;
    }

    /// <summary>
    ///     The global setting, as the installer saves it; an existing one (the operator may have chosen other
    ///     methods already) is never overwritten
    /// </summary>
    private static async Task InsertSettings(IRepository<Setting> repository)
    {
        var name = nameof(OrderApprovalSettings).ToLowerInvariant();
        var existing = await repository.FirstOrDefaultAsync(
            repository.Table.Where(x => x.Name == name && (x.StoreId == null || x.StoreId == "")));
        if (existing != null) return;

        await repository.InsertAsync(SettingExtensions.CreateSetting(new OrderApprovalSettings {
            PaymentMethodSystemNames = ["Payments.CashOnDelivery"]
        }));
    }

    /// <summary>
    ///     The store's default e-mail account when it still exists, otherwise the first one. Empty when there is none:
    ///     the sender then falls back to the default account of the settings at send time
    /// </summary>
    internal static async Task<string> GetEmailAccountId(IServiceProvider serviceProvider)
    {
        var emailAccountRepository = serviceProvider.GetRequiredService<IRepository<EmailAccount>>();
        var settingRepository = serviceProvider.GetRequiredService<IRepository<Setting>>();

        var settingName = nameof(EmailAccountSettings).ToLowerInvariant();
        var setting = await settingRepository.FirstOrDefaultAsync(
            settingRepository.Table.Where(x => x.Name == settingName && (x.StoreId == null || x.StoreId == "")));
        if (!string.IsNullOrEmpty(setting?.Metadata))
        {
            var defaultId = JsonSerializer.Deserialize<EmailAccountSettings>(setting.Metadata)?.DefaultEmailAccountId;
            if (!string.IsNullOrEmpty(defaultId))
            {
                var account = await emailAccountRepository.FirstOrDefaultAsync(
                    emailAccountRepository.Table.Where(x => x.Id == defaultId));
                if (account != null) return account.Id;
            }
        }

        var first = await emailAccountRepository.FirstOrDefaultAsync(emailAccountRepository.Table.OrderBy(x => x.Id));
        return first?.Id ?? string.Empty;
    }

    /// <summary>
    ///     The two templates, as the installer seeds them: same names, subjects and tokens
    /// </summary>
    public static IEnumerable<MessageTemplate> Templates(string emailAccountId)
    {
        yield return new MessageTemplate {
            Name = RequestedTemplateName,
            Subject = "{{Store.Name}}: order approval requested by {{OrderApproval.BuyerName}}",
            Body = Layout(
                "{% if OrderApproval.OrderNumber != '' %}Order #{{OrderApproval.OrderNumber}}{% else %}An order{% endif %} is waiting for your approval.",
                "Order approval requested",
                @"
<p>Hello {{Customer.FullName | Escape}},</p>
<p><strong>{{OrderApproval.BuyerName | Escape}}</strong> ({{OrderApproval.BuyerEmail | Escape}}) asked you to approve order{% if OrderApproval.OrderNumber != '' %} <strong>#{{OrderApproval.OrderNumber}}</strong>{% endif %} at <strong>{{Store.Name}}</strong>. The order is on hold until you decide.</p>"
                + Button("{{OrderApproval.Url}}", "Review the order") + @"
<p>The order contains:</p>"
                + "{{OrderApproval.ItemsHtml}}" + @"
<p style='text-align: right;'><strong>Order total: {{OrderApproval.Total}}</strong></p>"),
            IsActive = true,
            EmailAccountId = emailAccountId
        };
        yield return new MessageTemplate {
            Name = DecidedTemplateName,
            Subject = "{{Store.Name}}: your order approval request is {{OrderApproval.Status}}",
            Body = Layout(
                "Your order approval request has been decided.",
                "Order approval {{OrderApproval.Status}}",
                @"
<p>Hello {{OrderApproval.BuyerName | Escape}},</p>
<p>Your request to approve order{% if OrderApproval.OrderNumber != '' %} <strong>#{{OrderApproval.OrderNumber}}</strong>{% endif %} at <strong>{{Store.Name}}</strong> has been decided. Status: <strong>{{OrderApproval.Status}}</strong>.</p>
{% if OrderApproval.DecisionComment != null and OrderApproval.DecisionComment != '' %}
<p>Comment from the approver:</p>
<p style='padding: 0.8em 1em; background-color: #f4f4f5; border-radius: 6px;'>{{OrderApproval.DecisionComment}}</p>
{% endif %}"
                + Button("{{OrderApproval.OrderUrl}}", "View your order") + @"
<p>The order contains:</p>"
                + "{{OrderApproval.ItemsHtml}}" + @"
<p style='text-align: right;'><strong>Order total: {{OrderApproval.Total}}</strong></p>
<p>You can follow the request at <a href='{{OrderApproval.Url}}'>{{OrderApproval.Url}}</a>.</p>"),
            IsActive = true,
            EmailAccountId = emailAccountId
        };
    }

    internal static string Layout(string preheader, string heading, string content)
    {
        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8' />
<meta name='viewport' content='width=device-width, initial-scale=1.0' />
<title>{{{{Store.Name}}}}</title>
</head>
<body style='margin: 0; padding: 0; background-color: #f4f4f5;'>
<div style='display:none;max-height:0;overflow:hidden;'>{preheader}</div>
<table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='width: 100%; background-color: #f4f4f5;'>
<tr>
<td align='center' style='padding: 24px 16px;'>
<table role='presentation' width='600' cellpadding='0' cellspacing='0' style='width: 600px; max-width: 600px; background-color: #ffffff; border-radius: 8px;'>
<tr>
<td style='padding: 24px 32px; text-align: left;'>
<a href='{{{{Store.URL}}}}' style='font-size: 20px; font-weight: bold; color: #18181b; text-decoration: none;'>{{{{Store.Name}}}}</a>
</td>
</tr>
<tr>
<td style='padding: 0 32px;'>
<h1 style='font-size: 22px; color: #18181b; margin: 0 0 16px 0;'>{heading}</h1>
</td>
</tr>
<tr>
<td style='padding: 0 32px 24px 32px; font-size: 15px; line-height: 1.6; color: #3f3f46;'>
{content}
</td>
</tr>
<tr>
<td style='padding: 24px 32px; font-size: 12px; color: #71717a; border-top: 1px solid #e4e4e7;'>
{{% if Store.CompanyName != null and Store.CompanyName != '' %}}
{{{{Store.CompanyName}}}}<br />
{{% endif %}}
{{% if Store.CompanyAddress != null and Store.CompanyAddress != '' %}}
{{{{Store.CompanyAddress}}}}<br />
{{% endif %}}
{{% if Store.CompanyPhoneNumber != null and Store.CompanyPhoneNumber != '' %}}
{{{{Store.CompanyPhoneNumber}}}}<br />
{{% endif %}}
{{% if Store.CompanyEmail != null and Store.CompanyEmail != '' %}}
{{{{Store.CompanyEmail}}}}
{{% endif %}}
</td>
</tr>
</table>
</td>
</tr>
</table>
</body>
</html>";
    }

    internal static string Button(string url, string text)
    {
        return $@"
<table role='presentation' cellpadding='0' cellspacing='0' style='margin: 16px 0;'>
<tr>
<td style='border-radius: 6px; background-color: #18181b;'>
<a href='{url}' style='display: inline-block; padding: 12px 24px; font-size: 14px; font-weight: bold; color: #ffffff; text-decoration: none; border-radius: 6px;'>{text}</a>
</td>
</tr>
</table>";
    }
}
