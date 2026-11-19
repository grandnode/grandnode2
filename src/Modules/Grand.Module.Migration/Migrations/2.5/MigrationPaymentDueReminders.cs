using Grand.Data;
using Grand.Domain.Messages;
using Grand.Domain.Orders;
using Grand.Domain.Tasks;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Grand.Module.Migration.Migrations._2._5.MigrationOrderApproval;

namespace Grand.Module.Migration.Migrations._2._5;

/// <summary>
///     Payment due-date reminders: the daily task, the two customer e-mails and the index on the due date. The installer
///     seeds the same (the template bodies are repeated - a test keeps them equal), except that the task is added
///     disabled here: an existing shop may hold orders overdue for months, and the operator decides when their
///     customers are told. Idempotent: what exists is left alone
/// </summary>
public class MigrationPaymentDueReminders : IMigration
{
    private const string TaskName = "Payment due reminders";

    public int Priority => 3;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("8f8b7b1b-241b-4462-9856-c56ea52865de");
    public string Name => "Payment due reminders: task, e-mails and index 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationPaymentDueReminders>>();
        var success = true;

        async Task Step(string name, Func<Task> step)
        {
            try
            {
                await step();
            }
            catch (Exception ex)
            {
                logService.LogError(ex, "UpgradeProcess - PaymentDueReminders {Step} (2.5)", name);
                success = false;
            }
        }

        await Step("index", () => serviceProvider.GetRequiredService<IDatabaseContext>().CreateIndex(
            serviceProvider.GetRequiredService<IRepository<Order>>(),
            OrderBuilder<Order>.Create().Ascending(x => x.PaymentDueDateUtc), "PaymentDueDateUtc"));
        await Step("task", () => AddTask(serviceProvider));
        await Step("templates", () => AddTemplates(serviceProvider));
        return success;
    }

    private static async Task AddTask(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<ScheduleTask>>();
        if (!await repository.AnyAsync(repository.Table.Where(x => x.ScheduleTaskName == TaskName)))
            await repository.InsertAsync(new ScheduleTask {
                ScheduleTaskName = TaskName, Enabled = false, StopOnError = false, TimeInterval = 1440
            });
    }

    private static async Task AddTemplates(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<MessageTemplate>>();
        var emailAccountId = await GetEmailAccountId(serviceProvider);
        foreach (var template in Templates(emailAccountId))
        {
            var name = template.Name;
            if (await repository.FirstOrDefaultAsync(repository.Table.Where(x => x.Name == name)) == null)
                await repository.InsertAsync(template);
        }

        //a lookup made before the upgrade may have cached "no such template"
        var cache = serviceProvider.GetService<ICacheBase>();
        if (cache != null)
            await cache.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY);
    }

    private const string PaymentDueRows = @"<table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 0.4em 0;'><strong>Order number:</strong></td><td style='padding: 0.4em 0;'>{{Order.OrderNumber}}</td></tr>
                {% if Order.PurchaseOrderNumber != '' %}
                <tr><td style='padding: 0.4em 0;'><strong>Purchase order number:</strong></td><td style='padding: 0.4em 0;'>{{Order.PurchaseOrderNumber}}</td></tr>
                {% endif %}
                <tr><td style='padding: 0.4em 0;'><strong>Order date:</strong></td><td style='padding: 0.4em 0;'>{{Order.CreatedOn}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Order total:</strong></td><td style='padding: 0.4em 0;'>{{Order.Total}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Payment due date:</strong></td><td style='padding: 0.4em 0;'>{{Order.PaymentDueDate}}</td></tr>
                </table>";

    /// <summary>
    ///     The templates, as the installer seeds them: same names, subjects and tokens
    /// </summary>
    public static IList<MessageTemplate> Templates(string emailAccountId)
    {
        return new List<MessageTemplate> {
            new() {
                Name = "OrderPaymentDue.CustomerNotification",
                Subject = "{{Store.Name}} — Payment for order #{{Order.OrderNumber}} due on {{Order.PaymentDueDate}}",
                Body = Layout(
                    "A reminder: the payment for your order falls due soon.",
                    "Payment due soon",
                    @"<p>Hello {{Order.CustomerFullName}},</p>
                <p>This is a friendly reminder that the payment for order #{{Order.OrderNumber}} is due on <strong>{{Order.PaymentDueDate}}</strong>.</p>"
                    + PaymentDueRows
                    + Button("{{Order.OrderURLForCustomer}}", "View order details")
                    + @"<p>If you have already paid, please ignore this message.</p>"),
                IsActive = true,
                EmailAccountId = emailAccountId
            },
            new() {
                Name = "OrderPaymentOverdue.CustomerNotification",
                Subject = "{{Store.Name}} — Payment for order #{{Order.OrderNumber}} is overdue",
                Body = Layout(
                    "The payment for your order is overdue.",
                    "Payment overdue",
                    @"<p>Hello {{Order.CustomerFullName}},</p>
                <p>We have not received the payment for order #{{Order.OrderNumber}}, which was due on <strong>{{Order.PaymentDueDate}}</strong>. Please pay as soon as possible.</p>"
                    + PaymentDueRows
                    + Button("{{Order.OrderURLForCustomer}}", "View order details")
                    + @"<p>If you have already paid, please ignore this message. If you have any questions, just reply to this email.</p>"),
                IsActive = true,
                EmailAccountId = emailAccountId
            }
        };
    }
}
