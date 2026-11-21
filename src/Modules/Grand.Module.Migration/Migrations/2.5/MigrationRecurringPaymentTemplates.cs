using Grand.Data;
using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Grand.Module.Migration.Migrations._2._5.MigrationOrderApproval;

namespace Grand.Module.Migration.Migrations._2._5;

/// <summary>
///     Adds the e-mails for a recurring payment cancelled and for a cycle that failed, to the customer and to the
///     store. The installer creates the same for new installations (it is not referenced from here, so the bodies
///     are repeated below - a test keeps them equal). Idempotent: a template that exists (the operator may have
///     created or edited one) is never touched.
/// </summary>
public class MigrationRecurringPaymentTemplates : IMigration
{
    public int Priority => 2;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("4967620a-f03f-45bc-aa56-b53fba5d0862");
    public string Name => "Recurring payment e-mails 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationRecurringPaymentTemplates>>();

        try
        {
            var repository = serviceProvider.GetRequiredService<IRepository<MessageTemplate>>();
            var emailAccountId = await GetEmailAccountId(serviceProvider);
            foreach (var template in Templates(emailAccountId))
            {
                var existing =
                    await repository.FirstOrDefaultAsync(repository.Table.Where(x => x.Name == template.Name));
                if (existing == null)
                    await repository.InsertAsync(template);
            }

            //a lookup made before the upgrade may have cached "no such template"
            var cache = serviceProvider.GetService<ICacheBase>();
            if (cache != null)
                await cache.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - RecurringPaymentTemplates (2.5)");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     The templates, as the installer seeds them: same names, subjects and tokens
    /// </summary>
    public static IList<MessageTemplate> Templates(string emailAccountId)
    {
        return new List<MessageTemplate> {
            new() {
                Name = "RecurringPayment.Cancelled.CustomerNotification",
                Subject = "{{Store.Name}}: your recurring payment for order #{{Order.OrderNumber}} is cancelled",
                Body = Layout(
                    "Your recurring payment is cancelled.",
                    "Recurring payment cancelled",
                    @"<p>Hello {{Customer.FullName | Escape}},</p>
                <p>The recurring payment for order #{{Order.OrderNumber}} at {{Store.Name}} is cancelled. You will not be charged for further cycles.</p>
                <ul>{% for item in Order.OrderItems %}<li>{{item.ProductName}}</li>{% endfor %}</ul>
                <p>Every {{RecurringPayment.CycleLength}} {{RecurringPayment.CyclePeriod}}{% if RecurringPayment.TotalCycles > 0 %}, {{RecurringPayment.CyclesPaid}} of {{RecurringPayment.TotalCycles}} paid{% endif %}</p>
                " + Button("{{Order.OrderURLForCustomer}}", "View the order") + @"
                <p>To buy again, place a new order.</p>"),
                IsActive = true,
                EmailAccountId = emailAccountId
            },
            new() {
                Name = "RecurringPayment.Cancelled.StoreOwnerNotification",
                Subject = "{{Store.Name}}: recurring payment for order #{{Order.OrderNumber}} cancelled",
                Body = Layout(
                    "A recurring payment was cancelled.",
                    "Recurring payment cancelled",
                    @"<p>The recurring payment for order #{{Order.OrderNumber}} ({{Customer.Email | Escape}}) was cancelled. Cancelled by: {{RecurringPayment.CancelledBy}}.</p>
                <ul>{% for item in Order.OrderItems %}<li>{{item.ProductName}}</li>{% endfor %}</ul>
                <p>Every {{RecurringPayment.CycleLength}} {{RecurringPayment.CyclePeriod}}{% if RecurringPayment.TotalCycles > 0 %}, {{RecurringPayment.CyclesPaid}} of {{RecurringPayment.TotalCycles}} paid{% endif %}</p>"),
                IsActive = true,
                EmailAccountId = emailAccountId
            },
            new() {
                Name = "RecurringPayment.PaymentFailed.CustomerNotification",
                Subject = "{{Store.Name}}: the payment for order #{{Order.OrderNumber}} could not be taken",
                Body = Layout(
                    "A payment for your recurring order could not be taken.",
                    "Payment failed",
                    @"<p>Hello {{Customer.FullName | Escape}},</p>
                <p>The payment for the next cycle of your recurring payment for order #{{Order.OrderNumber}} could not be taken{% if RecurringPayment.LastError != '' %}: {{RecurringPayment.LastError}}{% endif %}.</p>
                <p>The payment provider may try again. Please check that your payment details are up to date.</p>
                <ul>{% for item in Order.OrderItems %}<li>{{item.ProductName}}</li>{% endfor %}</ul>
                " + Button("{{Order.OrderURLForCustomer}}", "View the order")),
                IsActive = true,
                EmailAccountId = emailAccountId
            },
            new() {
                Name = "RecurringPayment.PaymentFailed.StoreOwnerNotification",
                Subject = "{{Store.Name}}: a recurring payment for order #{{Order.OrderNumber}} failed",
                Body = Layout(
                    "A cycle of a recurring payment failed.",
                    "Recurring payment failed",
                    @"<p>A cycle of the recurring payment for order #{{Order.OrderNumber}} ({{Customer.Email | Escape}}) failed{% if RecurringPayment.LastError != '' %}: {{RecurringPayment.LastError}}{% endif %}.</p>
                <p>The payment provider may retry it.</p>
                <ul>{% for item in Order.OrderItems %}<li>{{item.ProductName}}</li>{% endfor %}</ul>
                <p>Every {{RecurringPayment.CycleLength}} {{RecurringPayment.CyclePeriod}}{% if RecurringPayment.TotalCycles > 0 %}, {{RecurringPayment.CyclesPaid}} of {{RecurringPayment.TotalCycles}} paid{% endif %}</p>"),
                IsActive = true,
                EmailAccountId = emailAccountId
            }
        };
    }
}
