using Grand.Domain.Messages;

namespace Grand.Module.Installer.Services.MessageTemplates;

public static partial class MessageTemplateSeed
{
    private static IEnumerable<MessageTemplate> OrdersTemplates(string ea)
    {
        return new List<MessageTemplate> {
            new() {
                Name = "OrderPlaced.CustomerNotification",
                Subject = "{{Store.Name}} — Order #{{Order.OrderNumber}} confirmed",
                Body = Layout(
                    "Thanks for your order — here is your receipt.",
                    "Order confirmed",
                    @"<p>Hello {{Order.CustomerFullName}},</p>
                <p>Thanks for shopping with {{Store.Name}}. We've received order #{{Order.OrderNumber}}, placed on {{Order.CreatedOn}}, and we'll email you again as soon as it ships.</p>"
                    + Button("{{Order.OrderURLForCustomer}}", "View order details")
                    + @"<table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 0.4em 0;'><strong>Order number:</strong></td><td style='padding: 0.4em 0;'>{{Order.OrderNumber}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Order date:</strong></td><td style='padding: 0.4em 0;'>{{Order.CreatedOn}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Shipping method:</strong></td><td style='padding: 0.4em 0;'>{{Order.ShippingMethod}}</td></tr>
                </table>
                <table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr>
                <td style='width:50%; vertical-align: top; padding-right: 8px;'><strong>Billing address</strong><br />{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />{{Order.BillingAddress1}}<br />{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />{{Order.BillingStateProvince}} {{Order.BillingCountry}}</td>
                <td style='width:50%; vertical-align: top;'><strong>Shipping address</strong><br />{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />{{Order.ShippingAddress1}}<br />{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}</td>
                </tr>
                </table>
                <p>Here's what you ordered:</p>"
                    + OrderProducts),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderPlaced.StoreOwnerNotification",
                Subject = "{{Store.Name}} — New order #{{Order.OrderNumber}} placed",
                Body = Layout(
                    "A customer just placed a new order.",
                    "New order placed",
                    @"<p>{{Order.CustomerFullName}} ({{Order.CustomerEmail}}) just placed an order.</p>
                <table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 0.4em 0;'><strong>Order number:</strong></td><td style='padding: 0.4em 0;'>{{Order.OrderNumber}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Order date:</strong></td><td style='padding: 0.4em 0;'>{{Order.CreatedOn}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Shipping method:</strong></td><td style='padding: 0.4em 0;'>{{Order.ShippingMethod}}</td></tr>
                </table>
                <table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr>
                <td style='width:50%; vertical-align: top; padding-right: 8px;'><strong>Billing address</strong><br />{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />{{Order.BillingAddress1}}<br />{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />{{Order.BillingStateProvince}} {{Order.BillingCountry}}</td>
                <td style='width:50%; vertical-align: top;'><strong>Shipping address</strong><br />{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />{{Order.ShippingAddress1}}<br />{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}</td>
                </tr>
                </table>"
                    + OrderProducts),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderPlaced.VendorNotification",
                Subject = "{{Store.Name}} — New order placed for your products",
                Body = Layout(
                    "A customer just placed a new order for your products.",
                    "New order placed",
                    @"<p>{{Customer.FullName}} ({{Customer.Email}}) just placed an order that includes your products.</p>
                <table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 0.4em 0;'><strong>Order number:</strong></td><td style='padding: 0.4em 0;'>{{Order.OrderNumber}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Order date:</strong></td><td style='padding: 0.4em 0;'>{{Order.CreatedOn}}</td></tr>
                </table>"
                    + OrderVendorProducts),
                //this template is disabled by default
                IsActive = false,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderPaid.StoreOwnerNotification",
                Subject = "{{Store.Name}} — Order #{{Order.OrderNumber}} paid",
                Body = Layout(
                    "An order has just been paid.",
                    "Order paid",
                    @"<p>Order #{{Order.OrderNumber}}, placed on {{Order.CreatedOn}}, has just been paid.</p>
                <table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 0.4em 0;'><strong>Order number:</strong></td><td style='padding: 0.4em 0;'>{{Order.OrderNumber}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Order date:</strong></td><td style='padding: 0.4em 0;'>{{Order.CreatedOn}}</td></tr>
                </table>"),
                //this template is disabled by default
                IsActive = false,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderPaid.CustomerNotification",
                Subject = "{{Store.Name}} — Order #{{Order.OrderNumber}} paid",
                Body = Layout(
                    "We've received your payment — here is your receipt.",
                    "Payment received",
                    @"<p>Hello {{Order.CustomerFullName}},</p>
                <p>Thanks for shopping with {{Store.Name}}. We've received your payment for order #{{Order.OrderNumber}}, placed on {{Order.CreatedOn}}.</p>"
                    + Button("{{Order.OrderURLForCustomer}}", "View order details")
                    + @"<table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 0.4em 0;'><strong>Order number:</strong></td><td style='padding: 0.4em 0;'>{{Order.OrderNumber}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Order date:</strong></td><td style='padding: 0.4em 0;'>{{Order.CreatedOn}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Shipping method:</strong></td><td style='padding: 0.4em 0;'>{{Order.ShippingMethod}}</td></tr>
                </table>
                <table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr>
                <td style='width:50%; vertical-align: top; padding-right: 8px;'><strong>Billing address</strong><br />{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />{{Order.BillingAddress1}}<br />{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />{{Order.BillingStateProvince}} {{Order.BillingCountry}}</td>
                <td style='width:50%; vertical-align: top;'><strong>Shipping address</strong><br />{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />{{Order.ShippingAddress1}}<br />{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}</td>
                </tr>
                </table>
                <p>Here's what you ordered:</p>"
                    + OrderProducts),
                //this template is disabled by default
                IsActive = false,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderPaid.VendorNotification",
                Subject = "{{Store.Name}} — Order #{{Order.OrderNumber}} paid",
                Body = Layout(
                    "An order for your products has just been paid.",
                    "Order paid",
                    @"<p>Order #{{Order.OrderNumber}}, placed on {{Order.CreatedOn}}, has just been paid.</p>
                <table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 0.4em 0;'><strong>Order number:</strong></td><td style='padding: 0.4em 0;'>{{Order.OrderNumber}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Order date:</strong></td><td style='padding: 0.4em 0;'>{{Order.CreatedOn}}</td></tr>
                </table>"
                    + OrderVendorProducts),
                //this template is disabled by default
                IsActive = false,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderCompleted.CustomerNotification",
                Subject = "{{Store.Name}} — Order #{{Order.OrderNumber}} completed",
                Body = Layout(
                    "Your order is complete.",
                    "Order completed",
                    @"<p>Hello {{Order.CustomerFullName}},</p>
                <p>Your order #{{Order.OrderNumber}}, placed on {{Order.CreatedOn}}, is now complete. Thanks for shopping with {{Store.Name}}.</p>"
                    + Button("{{Order.OrderURLForCustomer}}", "View order details")
                    + @"<table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 0.4em 0;'><strong>Order number:</strong></td><td style='padding: 0.4em 0;'>{{Order.OrderNumber}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Order date:</strong></td><td style='padding: 0.4em 0;'>{{Order.CreatedOn}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Shipping method:</strong></td><td style='padding: 0.4em 0;'>{{Order.ShippingMethod}}</td></tr>
                </table>
                <table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr>
                <td style='width:50%; vertical-align: top; padding-right: 8px;'><strong>Billing address</strong><br />{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />{{Order.BillingAddress1}}<br />{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />{{Order.BillingStateProvince}} {{Order.BillingCountry}}</td>
                <td style='width:50%; vertical-align: top;'><strong>Shipping address</strong><br />{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />{{Order.ShippingAddress1}}<br />{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}</td>
                </tr>
                </table>
                <p>Here's what you ordered:</p>"
                    + OrderProducts),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderCancelled.StoreOwnerNotification",
                Subject = "{{Store.Name}} — Order #{{Order.OrderNumber}} cancelled by customer",
                Body = Layout(
                    "A customer cancelled an order.",
                    "Order cancelled",
                    @"<p>A customer cancelled order #{{Order.OrderNumber}}, placed on {{Order.CreatedOn}}.</p>"
                    + Button("{{Order.OrderURLForCustomer}}", "View order details")
                    + @"<table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 0.4em 0;'><strong>Order number:</strong></td><td style='padding: 0.4em 0;'>{{Order.OrderNumber}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Order date:</strong></td><td style='padding: 0.4em 0;'>{{Order.CreatedOn}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Shipping method:</strong></td><td style='padding: 0.4em 0;'>{{Order.ShippingMethod}}</td></tr>
                </table>
                <table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr>
                <td style='width:50%; vertical-align: top; padding-right: 8px;'><strong>Billing address</strong><br />{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />{{Order.BillingAddress1}}<br />{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />{{Order.BillingStateProvince}} {{Order.BillingCountry}}</td>
                <td style='width:50%; vertical-align: top;'><strong>Shipping address</strong><br />{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />{{Order.ShippingAddress1}}<br />{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}</td>
                </tr>
                </table>"
                    + OrderProducts),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderCancelled.CustomerNotification",
                Subject = "{{Store.Name}} — Order #{{Order.OrderNumber}} cancelled",
                Body = Layout(
                    "Your order has been cancelled.",
                    "Order cancelled",
                    @"<p>Hello {{Order.CustomerFullName}},</p>
                <p>Your order #{{Order.OrderNumber}}, placed on {{Order.CreatedOn}}, has been cancelled.</p>"
                    + Button("{{Order.OrderURLForCustomer}}", "View order details")
                    + @"<table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 0.4em 0;'><strong>Order number:</strong></td><td style='padding: 0.4em 0;'>{{Order.OrderNumber}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Order date:</strong></td><td style='padding: 0.4em 0;'>{{Order.CreatedOn}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Shipping method:</strong></td><td style='padding: 0.4em 0;'>{{Order.ShippingMethod}}</td></tr>
                </table>
                <table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr>
                <td style='width:50%; vertical-align: top; padding-right: 8px;'><strong>Billing address</strong><br />{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />{{Order.BillingAddress1}}<br />{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />{{Order.BillingStateProvince}} {{Order.BillingCountry}}</td>
                <td style='width:50%; vertical-align: top;'><strong>Shipping address</strong><br />{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />{{Order.ShippingAddress1}}<br />{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}</td>
                </tr>
                </table>"
                    + OrderProducts),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderCancelled.VendorNotification",
                Subject = "{{Store.Name}} — Order #{{Order.OrderNumber}} cancelled",
                Body = Layout(
                    "An order has been cancelled.",
                    "Order cancelled",
                    @"<p>Order #{{Order.OrderNumber}}, placed on {{Order.CreatedOn}}, has been cancelled.</p>
                <table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 0.4em 0;'><strong>Order number:</strong></td><td style='padding: 0.4em 0;'>{{Order.OrderNumber}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Order date:</strong></td><td style='padding: 0.4em 0;'>{{Order.CreatedOn}}</td></tr>
                </table>"),
                IsActive = false,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderRefunded.CustomerNotification",
                Subject = "{{Store.Name}} — Order #{{Order.OrderNumber}} refunded",
                Body = Layout(
                    "Your order has been refunded.",
                    "Order refunded",
                    @"<p>Hello {{Order.CustomerFullName}},</p>
                <p>Thanks for shopping with {{Store.Name}}. Order #{{Order.OrderNumber}} has been refunded. Please allow 7-14 days for the refund to be reflected in your account.</p>"
                    + Button("{{Order.OrderURLForCustomer}}", "View order details")
                    + @"<table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 0.4em 0;'><strong>Order number:</strong></td><td style='padding: 0.4em 0;'>{{Order.OrderNumber}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Order date:</strong></td><td style='padding: 0.4em 0;'>{{Order.CreatedOn}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Amount refunded:</strong></td><td style='padding: 0.4em 0;'>{{Order.AmountRefunded}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Shipping method:</strong></td><td style='padding: 0.4em 0;'>{{Order.ShippingMethod}}</td></tr>
                </table>
                <table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr>
                <td style='width:50%; vertical-align: top; padding-right: 8px;'><strong>Billing address</strong><br />{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />{{Order.BillingAddress1}}<br />{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />{{Order.BillingStateProvince}} {{Order.BillingCountry}}</td>
                <td style='width:50%; vertical-align: top;'><strong>Shipping address</strong><br />{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />{{Order.ShippingAddress1}}<br />{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}</td>
                </tr>
                </table>
                <p>Here's what you ordered:</p>"
                    + OrderProducts),
                //this template is disabled by default
                IsActive = false,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderRefunded.StoreOwnerNotification",
                Subject = "{{Store.Name}} — Order #{{Order.OrderNumber}} refunded",
                Body = Layout(
                    "An order has just been refunded.",
                    "Order refunded",
                    @"<p>Order #{{Order.OrderNumber}}, placed on {{Order.CreatedOn}}, has just been refunded.</p>
                <table role='presentation' style='width:100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 0.4em 0;'><strong>Order number:</strong></td><td style='padding: 0.4em 0;'>{{Order.OrderNumber}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Order date:</strong></td><td style='padding: 0.4em 0;'>{{Order.CreatedOn}}</td></tr>
                <tr><td style='padding: 0.4em 0;'><strong>Amount refunded:</strong></td><td style='padding: 0.4em 0;'>{{Order.AmountRefunded}}</td></tr>
                </table>"),
                //this template is disabled by default
                IsActive = false,
                EmailAccountId = ea
            },
            new() {
                Name = "Customer.NewOrderNote",
                Subject = "{{Store.Name}} — New note on order #{{Order.OrderNumber}}",
                Body = Layout(
                    "A new note has been added to your order.",
                    "New order note",
                    @"<p>Hello {{Customer.FullName}},</p>
                <p>A new note has been added to order #{{Order.OrderNumber}}:</p>
                <p>""{{Order.NewNoteText}}""</p>"
                    + Button("{{Order.OrderURLForCustomer}}", "View order")),
                IsActive = true,
                EmailAccountId = ea
            },
        };
    }
}
