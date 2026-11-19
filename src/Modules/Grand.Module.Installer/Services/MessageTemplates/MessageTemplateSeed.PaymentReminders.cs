using Grand.Domain.Messages;

namespace Grand.Module.Installer.Services.MessageTemplates;

public static partial class MessageTemplateSeed
{
    /// <summary>
    ///     The order, its purchase order number and what is due, by when
    /// </summary>
    private const string PaymentDueRows = @"<table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 0.4em 0;'><strong>Order number:</strong></td><td style='padding: 0.4em 0;'>{{Order.OrderNumber}}</td></tr>
                {% if Order.PurchaseOrderNumber != '' %}
                <tr><td style='padding: 0.4em 0;'><strong>Purchase order number:</strong></td><td style='padding: 0.4em 0;'>{{Order.PurchaseOrderNumber}}</td></tr>
                {% endif %}
                <tr><td style='padding: 0.4em 0;'><strong>Order date:</strong></td><td style='padding: 0.4em 0;'>{{Order.CreatedOn}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Order total:</strong></td><td style='padding: 0.4em 0;'>{{Order.Total}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Payment due date:</strong></td><td style='padding: 0.4em 0;'>{{Order.PaymentDueDate}}</td></tr>
                </table>";

    private static IEnumerable<MessageTemplate> PaymentReminderTemplates(string ea)
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
                EmailAccountId = ea
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
                EmailAccountId = ea
            }
        };
    }
}
