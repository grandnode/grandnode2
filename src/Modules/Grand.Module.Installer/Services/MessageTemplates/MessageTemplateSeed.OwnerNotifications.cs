using Grand.Domain.Messages;

namespace Grand.Module.Installer.Services.MessageTemplates;

public static partial class MessageTemplateSeed
{
    private static IEnumerable<MessageTemplate> OwnerNotificationTemplates(string ea)
    {
        return new List<MessageTemplate> {
            new() {
                Name = "NewCustomer.Notification",
                Subject = "{{Store.Name}}. New customer registration",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nA new customer registered with your store. Below are the customer's details:<br />\r\nFull name: {{Customer.FullName}}<br />\r\nEmail: {{Customer.Email}}</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "CustomerDelete.StoreOwnerNotification",
                Subject = "{{Store.Name}}. Customer has been deleted.",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> ,<br />\r\n{{Customer.FullName}} ({{Customer.Email}}) has just deleted from your database. </p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "QuantityBelow.StoreOwnerNotification",
                Subject = "{{Store.Name}}. Quantity below notification. {{Product.Name}}",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\n{{Product.Name}} (ID: {{Product.Id}}) low quantity. <br />\r\n<br />\r\nQuantity: {{Product.StockQuantity}}<br />\r\n</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "QuantityBelow.AttributeCombination.StoreOwnerNotification",
                Subject = "{{Store.Name}}. Quantity below notification. {{Product.Name}}",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\n{{Product.Name}} (ID: {{Product.Id}}) low quantity. <br />\r\n{{AttributeCombination.Formatted}}<br />\r\nQuantity: {{AttributeCombination.StockQuantity}}<br />\r\n</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "VendorAccountApply.StoreOwnerNotification",
                Subject = "{{Store.Name}}. New vendor account submitted.",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\n{{Customer.FullName}} ({{Customer.Email}}) has just submitted for a vendor account. Details are below:<br />\r\nVendor name: {{Vendor.Name}}<br />\r\nVendor email: {{Vendor.Email}}<br />\r\n<br />\r\nYou can activate it in admin area.</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "VendorInformationChange.StoreOwnerNotification",
                Subject = "{{Store.Name}}. Vendor {{Vendor.Name}} changed provided information",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\n{{Vendor.Name}} changed provided information.</p>",
                IsActive = false,
                EmailAccountId = ea
            },
            new() {
                Name = "Customer.OutOfStock",
                Subject = "{{Store.Name}}. Back in stock notification",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Customer.FullName}}, <br />\r\nProduct <a target=\"_blank\" href=\"{{OutOfStockSubscription.ProductUrl}}\">{{OutOfStockSubscription.ProductName}}</a> is in stock.</p>",
                IsActive = true,
                EmailAccountId = ea
            },
        };
    }
}
