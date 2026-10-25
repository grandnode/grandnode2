using Grand.Domain.Messages;

namespace Grand.Module.Installer.Services.MessageTemplates;

public static partial class MessageTemplateSeed
{
    private static IEnumerable<MessageTemplate> OrderApprovalTemplates(string ea)
    {
        return new List<MessageTemplate> {
            new() {
                Name = "OrderApproval.Requested",
                Subject = "{{Store.Name}} — Order approval requested by {{OrderApproval.BuyerName}}",
                Body = Layout(
                    "An order is waiting for your approval.",
                    "Order approval requested",
                    @"
                <p>Hello {{Customer.FullName | Escape}},</p>
                <p><strong>{{OrderApproval.BuyerName | Escape}}</strong> ({{OrderApproval.BuyerEmail | Escape}}) asked you to approve an order at <strong>{{Store.Name}}</strong>. It will not be placed until you decide.</p>"
                    + Button("{{OrderApproval.Url}}", "Review the order") + @"
                <p>The order contains:</p>"
                    + "{{OrderApproval.ItemsHtml}}" + @"
                <p style='text-align: right;'><strong>Order total: {{OrderApproval.Total}}</strong></p>"),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderApproval.Decided",
                Subject = "{{Store.Name}} — Your order approval request: {{OrderApproval.Status}}",
                Body = Layout(
                    "Your order approval request has been decided.",
                    "Order approval {{OrderApproval.Status}}",
                    @"
                <p>Hello {{Customer.FullName | Escape}},</p>
                <p>Your request to approve an order at <strong>{{Store.Name}}</strong> has been decided. Status: <strong>{{OrderApproval.Status}}</strong>.</p>
                {% if OrderApproval.DecisionComment != null and OrderApproval.DecisionComment != '' %}
                <p>Comment from the approver:</p>
                <p style='padding: 0.8em 1em; background-color: #f4f4f5; border-radius: 6px;'>{{OrderApproval.DecisionComment}}</p>
                {% endif %}"
                    + Button("{{OrderApproval.CartUrl}}", "Go to your cart") + @"
                <p>The order contains:</p>"
                    + "{{OrderApproval.ItemsHtml}}" + @"
                <p style='text-align: right;'><strong>Order total: {{OrderApproval.Total}}</strong></p>
                <p>You can follow the request at <a href='{{OrderApproval.Url}}'>{{OrderApproval.Url}}</a>.</p>"),
                IsActive = true,
                EmailAccountId = ea
            }
        };
    }
}
