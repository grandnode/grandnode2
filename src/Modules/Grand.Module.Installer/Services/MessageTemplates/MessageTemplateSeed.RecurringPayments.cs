using Grand.Domain.Messages;

namespace Grand.Module.Installer.Services.MessageTemplates;

public static partial class MessageTemplateSeed
{
    private static IEnumerable<MessageTemplate> RecurringPaymentTemplates(string ea)
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
                EmailAccountId = ea
            },
            new() {
                Name = "RecurringPayment.Cancelled.StoreOwnerNotification",
                Subject = "{{Store.Name}}: recurring payment for order #{{Order.OrderNumber}} cancelled",
                Body = Layout(
                    "A recurring payment was cancelled.",
                    "Recurring payment cancelled",
                    @"<p>The recurring payment for order #{{Order.OrderNumber}} ({{Customer.Email | Escape}}) was cancelled by {{RecurringPayment.CancelledBy}}.</p>
                <ul>{% for item in Order.OrderItems %}<li>{{item.ProductName}}</li>{% endfor %}</ul>
                <p>Every {{RecurringPayment.CycleLength}} {{RecurringPayment.CyclePeriod}}{% if RecurringPayment.TotalCycles > 0 %}, {{RecurringPayment.CyclesPaid}} of {{RecurringPayment.TotalCycles}} paid{% endif %}</p>"),
                IsActive = true,
                EmailAccountId = ea
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
                EmailAccountId = ea
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
                EmailAccountId = ea
            }
        };
    }
}
