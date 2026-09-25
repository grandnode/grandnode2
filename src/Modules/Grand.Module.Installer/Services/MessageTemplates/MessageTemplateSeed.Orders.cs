using Grand.Domain.Messages;

namespace Grand.Module.Installer.Services.MessageTemplates;

public static partial class MessageTemplateSeed
{
    private static IEnumerable<MessageTemplate> OrdersTemplates(string ea)
    {
        return new List<MessageTemplate> {
            new() {
                Name = "OrderPlaced.CustomerNotification",
                Subject = "Order receipt from {{Store.Name}}.",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Order.CustomerFullName}}, <br />\r\nThanks for buying from <a href=\"{{Store.URL}}\">{{Store.Name}}</a>. Below is the summary of the order. <br />\r\n<br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nOrder Details: <a target=\"_blank\" href=\"{{Order.OrderURLForCustomer}}\">{{Order.OrderURLForCustomer}}</a><br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nBilling Address<br />\r\n{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />\r\n{{Order.BillingAddress1}}<br />\r\n{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />\r\n{{Order.BillingStateProvince}} {{Order.BillingCountry}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nShipping Address<br />\r\n{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />\r\n{{Order.ShippingAddress1}}<br />\r\n{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />\r\n{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}<br />\r\n<br />\r\nShipping Method: {{Order.ShippingMethod}}<br />\r\n<br />\r\n" +
                    OrderProducts + "</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderPlaced.StoreOwnerNotification",
                Subject = "{{Store.Name}}. Purchase Receipt for Order #{{Order.OrderNumber}}",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\n{{Order.CustomerFullName}} ({{Order.CustomerEmail}}) has just placed an order from your store. Below is the summary of the order. <br />\r\n<br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nBilling Address<br />\r\n{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />\r\n{{Order.BillingAddress1}}<br />\r\n{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />\r\n{{Order.BillingStateProvince}} {{Order.BillingCountry}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nShipping Address<br />\r\n{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />\r\n{{Order.ShippingAddress1}}<br />\r\n{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />\r\n{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}<br />\r\n<br />\r\nShipping Method: {{Order.ShippingMethod}}<br />\r\n<br />\r\n" +
                    OrderProducts + "</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderPlaced.VendorNotification",
                Subject = "{{Store.Name}}. Order placed",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\n{{Customer.FullName}} ({{Customer.Email}}) has just placed an order. <br />\r\n<br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n" +
                    OrderVendorProducts + "</p>",
                //this template is disabled by default
                IsActive = false,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderPaid.StoreOwnerNotification",
                Subject = "{{Store.Name}}. Order #{{Order.OrderNumber}} paid",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nOrder #{{Order.OrderNumber}} has been just paid<br />\r\nDate Ordered: {{Order.CreatedOn}}</p>",
                //this template is disabled by default
                IsActive = false,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderPaid.CustomerNotification",
                Subject = "{{Store.Name}}. Order #{{Order.OrderNumber}} paid",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Order.CustomerFullName}}, <br />\r\nThanks for buying from <a href=\"{{Store.URL}}\">{{Store.Name}}</a>. Order #{{Order.OrderNumber}} has been just paid. Below is the summary of the order. <br />\r\n<br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nOrder Details: <a href=\"{{Order.OrderURLForCustomer}}\" target=\"_blank\">{{Order.OrderURLForCustomer}}</a><br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nBilling Address<br />\r\n{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />\r\n{{Order.BillingAddress1}}<br />\r\n{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />\r\n{{Order.BillingStateProvince}} {{Order.BillingCountry}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nShipping Address<br />\r\n{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />\r\n{{Order.ShippingAddress1}}<br />\r\n{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />\r\n{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}<br />\r\n<br />\r\nShipping Method: {{Order.ShippingMethod}}<br />\r\n<br />\r\n" +
                    OrderProducts + "</p>",
                //this template is disabled by default
                IsActive = false,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderPaid.VendorNotification",
                Subject = "{{Store.Name}}. Order #{{Order.OrderNumber}} paid",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nOrder #{{Order.OrderNumber}} has been just paid. <br />\r\n<br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n" +
                    OrderVendorProducts + "</p>",
                //this template is disabled by default
                IsActive = false,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderCompleted.CustomerNotification",
                Subject = "{{Store.Name}}. Your order completed",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Order.CustomerFullName}}, <br />\r\nYour order has been completed. Below is the summary of the order. <br />\r\n<br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nOrder Details: <a target=\"_blank\" href=\"{{Order.OrderURLForCustomer}}\">{{Order.OrderURLForCustomer}}</a><br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nBilling Address<br />\r\n{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />\r\n{{Order.BillingAddress1}}<br />\r\n{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />\r\n{{Order.BillingStateProvince}} {{Order.BillingCountry}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nShipping Address<br />\r\n{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />\r\n{{Order.ShippingAddress1}}<br />\r\n{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />\r\n{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}<br />\r\n<br />\r\nShipping Method: {{Order.ShippingMethod}}<br />\r\n<br />\r\n" +
                    OrderProducts + "</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderCancelled.StoreOwnerNotification",
                Subject = "{{Store.Name}}. Customer cancelled an order",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\n<br />\r\nCustomer cancelled an order. Below is the summary of the order. <br />\r\n<br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nOrder Details: <a target=\"_blank\" href=\"{{Order.OrderURLForCustomer}}\">{{Order.OrderURLForCustomer}}</a><br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nBilling Address<br />\r\n{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />\r\n{{Order.BillingAddress1}}<br />\r\n{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />\r\n{{Order.BillingStateProvince}} {{Order.BillingCountry}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nShipping Address<br />\r\n{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />\r\n{{Order.ShippingAddress1}}<br />\r\n{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />\r\n{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}<br />\r\n<br />\r\nShipping Method: {{Order.ShippingMethod}}<br />\r\n<br />\r\n" +
                    OrderProducts + "</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderCancelled.CustomerNotification",
                Subject = "{{Store.Name}}. Your order cancelled",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Order.CustomerFullName}}, <br />\r\nYour order has been cancelled. Below is the summary of the order. <br />\r\n<br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nOrder Details: <a target=\"_blank\" href=\"{{Order.OrderURLForCustomer}}\">{{Order.OrderURLForCustomer}}</a><br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nBilling Address<br />\r\n{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />\r\n{{Order.BillingAddress1}}<br />\r\n{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />\r\n{{Order.BillingStateProvince}} {{Order.BillingCountry}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nShipping Address<br />\r\n{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />\r\n{{Order.ShippingAddress1}}<br />\r\n{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />\r\n{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}<br />\r\n<br />\r\nShipping Method: {{Order.ShippingMethod}}<br />\r\n<br />\r\n" +
                    OrderProducts + "</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderCancelled.VendorNotification",
                Subject = "{{Store.Name}}. Order #{{Order.OrderNumber}} cancelled",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br /><br />Order #{{Order.OrderNumber}} has been cancelled. <br /><br />Order Number: {{Order.OrderNumber}} <br />   Date Ordered: {{Order.CreatedOn}} <br /><br /> ",
                IsActive = false,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderRefunded.CustomerNotification",
                Subject = "{{Store.Name}}. Order #{{Order.OrderNumber}} refunded",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Order.CustomerFullName}}, <br />\r\nThanks for buying from <a href=\"{{Store.URL}}\">{{Store.Name}}</a>. Order #{{Order.OrderNumber}} has been has been refunded. Please allow 7-14 days for the refund to be reflected in your account.<br />\r\n<br />\r\nAmount refunded: {{Order.AmountRefunded}}<br />\r\n<br />\r\nBelow is the summary of the order. <br />\r\n<br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nOrder Details: <a href=\"{{Order.OrderURLForCustomer}}\" target=\"_blank\">{{Order.OrderURLForCustomer}}</a><br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nBilling Address<br />\r\n{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />\r\n{{Order.BillingAddress1}}<br />\r\n{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />\r\n{{Order.BillingStateProvince}} {{Order.BillingCountry}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nShipping Address<br />\r\n{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />\r\n{{Order.ShippingAddress1}}<br />\r\n{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />\r\n{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}<br />\r\n<br />\r\nShipping Method: {{Order.ShippingMethod}}<br />\r\n<br />\r\n" +
                    OrderProducts + "</p>",
                //this template is disabled by default
                IsActive = false,
                EmailAccountId = ea
            },
            new() {
                Name = "OrderRefunded.StoreOwnerNotification",
                Subject = "{{Store.Name}}. Order #{{Order.OrderNumber}} refunded",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nOrder #{{Order.OrderNumber}} has been just refunded<br />\r\n<br />\r\nAmount refunded: {{Order.AmountRefunded}}<br />\r\n<br />\r\nDate Ordered: {{Order.CreatedOn}}</p>",
                //this template is disabled by default
                IsActive = false,
                EmailAccountId = ea
            },
            new() {
                Name = "Customer.NewOrderNote",
                Subject = "{{Store.Name}}. New order note has been added",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Customer.FullName}}, <br />\r\nNew order note has been added to your account:<br />\r\n\"{{Order.NewNoteText}}\".<br />\r\n<a target=\"_blank\" href=\"{{Order.OrderURLForCustomer}}\">{{Order.OrderURLForCustomer}}</a></p>",
                IsActive = true,
                EmailAccountId = ea
            },
        };
    }
}
