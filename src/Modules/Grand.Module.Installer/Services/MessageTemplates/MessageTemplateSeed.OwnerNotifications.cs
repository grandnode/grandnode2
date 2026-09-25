using Grand.Domain.Messages;

namespace Grand.Module.Installer.Services.MessageTemplates;

public static partial class MessageTemplateSeed
{
    private static IEnumerable<MessageTemplate> OwnerNotificationTemplates(string ea)
    {
        return new List<MessageTemplate> {
            new() {
                Name = "NewCustomer.Notification",
                Subject = "{{Store.Name}} — New customer registered",
                Body = Layout(
                    "A new customer just registered on your store.",
                    "New customer registration",
                    """
                    <p>A new customer registered with {{Store.Name}}. Details are below.</p>
                    <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                    <tr><td style='padding: 0.3em 0; color: #71717a;'>Full name</td><td style='padding: 0.3em 0; text-align: right;'>{{Customer.FullName}}</td></tr>
                    <tr><td style='padding: 0.3em 0; color: #71717a;'>Email</td><td style='padding: 0.3em 0; text-align: right;'>{{Customer.Email}}</td></tr>
                    </table>
                    """),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "CustomerDelete.StoreOwnerNotification",
                Subject = "{{Store.Name}} — Customer account deleted",
                Body = Layout(
                    "A customer account was removed from your store.",
                    "Customer account deleted",
                    """
                    <p>The customer below has just been deleted from your database.</p>
                    <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                    <tr><td style='padding: 0.3em 0; color: #71717a;'>Full name</td><td style='padding: 0.3em 0; text-align: right;'>{{Customer.FullName}}</td></tr>
                    <tr><td style='padding: 0.3em 0; color: #71717a;'>Email</td><td style='padding: 0.3em 0; text-align: right;'>{{Customer.Email}}</td></tr>
                    </table>
                    """),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "QuantityBelow.StoreOwnerNotification",
                Subject = "{{Store.Name}} — Low stock: {{Product.Name}}",
                Body = Layout(
                    "A product on your store has dropped below its stock threshold.",
                    "Low stock alert",
                    """
                    <p>{{Product.Name}} has fallen below its low stock threshold.</p>
                    <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                    <tr><td style='padding: 0.3em 0; color: #71717a;'>Product</td><td style='padding: 0.3em 0; text-align: right;'>{{Product.Name}}</td></tr>
                    <tr><td style='padding: 0.3em 0; color: #71717a;'>Product ID</td><td style='padding: 0.3em 0; text-align: right;'>{{Product.Id}}</td></tr>
                    <tr><td style='padding: 0.3em 0; color: #71717a;'>Quantity</td><td style='padding: 0.3em 0; text-align: right;'>{{Product.StockQuantity}}</td></tr>
                    </table>
                    """ + Button("{{Product.ProductURLForCustomer}}", "View product")),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "QuantityBelow.AttributeCombination.StoreOwnerNotification",
                Subject = "{{Store.Name}} — Low stock: {{Product.Name}}",
                Body = Layout(
                    "A product variant on your store has dropped below its stock threshold.",
                    "Low stock alert",
                    """
                    <p>A variant of {{Product.Name}} has fallen below its low stock threshold.</p>
                    <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                    <tr><td style='padding: 0.3em 0; color: #71717a;'>Product</td><td style='padding: 0.3em 0; text-align: right;'>{{Product.Name}}</td></tr>
                    <tr><td style='padding: 0.3em 0; color: #71717a;'>Product ID</td><td style='padding: 0.3em 0; text-align: right;'>{{Product.Id}}</td></tr>
                    <tr><td style='padding: 0.3em 0; color: #71717a;'>Variant</td><td style='padding: 0.3em 0; text-align: right;'>{{AttributeCombination.Formatted}}</td></tr>
                    <tr><td style='padding: 0.3em 0; color: #71717a;'>Quantity</td><td style='padding: 0.3em 0; text-align: right;'>{{AttributeCombination.StockQuantity}}</td></tr>
                    </table>
                    """ + Button("{{Product.ProductURLForCustomer}}", "View product")),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "VendorAccountApply.StoreOwnerNotification",
                Subject = "{{Store.Name}} — New vendor application: {{Vendor.Name}}",
                Body = Layout(
                    "A customer applied for a vendor account on your store.",
                    "New vendor application",
                    """
                    <p>{{Customer.FullName}} ({{Customer.Email}}) has submitted an application for a vendor account. Details are below.</p>
                    <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                    <tr><td style='padding: 0.3em 0; color: #71717a;'>Vendor name</td><td style='padding: 0.3em 0; text-align: right;'>{{Vendor.Name}}</td></tr>
                    <tr><td style='padding: 0.3em 0; color: #71717a;'>Vendor email</td><td style='padding: 0.3em 0; text-align: right;'>{{Vendor.Email}}</td></tr>
                    </table>
                    <p>You can activate this account from your admin area.</p>
                    """),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "VendorInformationChange.StoreOwnerNotification",
                Subject = "{{Store.Name}} — Vendor {{Vendor.Name}} updated their information",
                Body = Layout(
                    "A vendor updated their store information.",
                    "Vendor information changed",
                    """
                    <p>{{Vendor.Name}} has changed their provided information. Review the update in your admin area.</p>
                    """),
                IsActive = false,
                EmailAccountId = ea
            },
            new() {
                Name = "Customer.OutOfStock",
                Subject = "{{Store.Name}} — {{OutOfStockSubscription.ProductName}} is back in stock",
                Body = Layout(
                    "The product you were waiting for is available again.",
                    "Back in stock",
                    """
                    <p>Hello {{Customer.FullName}},</p>
                    <p>Good news — {{OutOfStockSubscription.ProductName}} is back in stock at {{Store.Name}}.</p>
                    """ + Button("{{OutOfStockSubscription.ProductUrl}}", "View product")),
                IsActive = true,
                EmailAccountId = ea
            },
        };
    }
}
