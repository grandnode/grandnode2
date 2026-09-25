using Grand.Domain.Messages;

namespace Grand.Module.Installer.Services.MessageTemplates;

public static partial class MessageTemplateSeed
{
    private static IEnumerable<MessageTemplate> ShippingTemplates(string ea)
    {
        return new List<MessageTemplate> {
            new() {
                Name = "ShipmentSent.CustomerNotification",
                Subject = "Your order from {{Store.Name}} has been shipped.",
                Body =
                    "<p><a href=\"{{Store.URL}}\"> {{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Order.CustomerFullName}}!, <br />\r\nGood news! You order has been shipped. <br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nOrder Details: <a href=\"{{Order.OrderURLForCustomer}}\" target=\"_blank\">{{Order.OrderURLForCustomer}}</a><br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nBilling Address<br />\r\n{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />\r\n{{Order.BillingAddress1}}<br />\r\n{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />\r\n{{Order.BillingStateProvince}} {{Order.BillingCountry}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nShipping Address<br />\r\n{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />\r\n{{Order.ShippingAddress1}}<br />\r\n{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />\r\n{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}<br />\r\n<br />\r\nShipping Method: {{Order.ShippingMethod}} <br />\r\n <br />\r\n Shipped Products: <br />\r\n <br />\r\n" +
                    ShipmentProducts + "</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "ShipmentDelivered.CustomerNotification",
                Subject = "Your order from {{Store.Name}} has been delivered.",
                Body =
                    "<p><a href=\"{{Store.URL}}\"> {{Store.Name}}</a> <br />\r\n <br />\r\n Hello {{Order.CustomerFullName}}, <br />\r\n Good news! You order has been delivered. <br />\r\n Order Number: {{Order.OrderNumber}}<br />\r\n Order Details: <a href=\"{{Order.OrderURLForCustomer}}\" target=\"_blank\">{{Order.OrderURLForCustomer}}</a><br />\r\n Date Ordered: {{Order.CreatedOn}}<br />\r\n <br />\r\n <br />\r\n <br />\r\n Billing Address<br />\r\n {{Order.BillingFirstName}} {{Order.BillingLastName}}<br />\r\n {{Order.BillingAddress1}}<br />\r\n {{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />\r\n {{Order.BillingStateProvince}} {{Order.BillingCountry}}<br />\r\n <br />\r\n <br />\r\n <br />\r\n Shipping Address<br />\r\n {{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />\r\n {{Order.ShippingAddress1}}<br />\r\n {{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />\r\n {{Order.ShippingStateProvince}} {{Order.ShippingCountry}}<br />\r\n <br />\r\n Shipping Method: {{Order.ShippingMethod}} <br />\r\n <br />\r\n Delivered Products: <br />\r\n <br />\r\n" +
                    ShipmentProducts + "</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "NewMerchandiseReturn.CustomerNotification",
                Subject = "{{Store.Name}}. New merchandise return.",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Customer.FullName}}!<br />\r\n You have just submitted a new merchandise return. Details are below:<br />\r\nRequest ID: {{MerchandiseReturn.ReturnNumber}}<br />\r\nCustomer comments: {{MerchandiseReturn.CustomerComment}}<br />\r\n<br />\r\nPickup date: {{MerchandiseReturn.PickupDate}}<br />\r\n<br />\r\nPickup address:<br />\r\n{{MerchandiseReturn.PickupAddressFirstName}} {{MerchandiseReturn.PickupAddressLastName}}<br />\r\n{{MerchandiseReturn.PickupAddressAddress1}}<br />\r\n{{MerchandiseReturn.PickupAddressCity}} {{MerchandiseReturn.PickupAddressZipPostalCode}}<br />\r\n{{MerchandiseReturn.PickupAddressStateProvince}} {{MerchandiseReturn.PickupAddressCountry}}<br />\r\n</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "NewMerchandiseReturn.StoreOwnerNotification",
                Subject = "{{Store.Name}}. New merchandise return.",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\n{{Customer.FullName}} has just submitted a new merchandise return. Details are below:<br />\r\nRequest ID: {{MerchandiseReturn.ReturnNumber}}<br />\r\nCustomer comments: {{MerchandiseReturn.CustomerComment}}<br />\r\n<br />\r\nPickup date: {{MerchandiseReturn.PickupDate}}<br />\r\n<br />\r\nPickup address:<br />\r\n{{MerchandiseReturn.PickupAddressFirstName}} {{MerchandiseReturn.PickupAddressLastName}}<br />\r\n{{MerchandiseReturn.PickupAddressAddress1}}<br />\r\n{{MerchandiseReturn.PickupAddressCity}} {{MerchandiseReturn.PickupAddressZipPostalCode}}<br />\r\n{{MerchandiseReturn.PickupAddressStateProvince}} {{MerchandiseReturn.PickupAddressCountry}}<br />\r\n</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "MerchandiseReturnStatusChanged.CustomerNotification",
                Subject = "{{Store.Name}}. Merchandise return status was changed.",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Customer.FullName}},<br />\r\nYour merchandise return #{{MerchandiseReturn.ReturnNumber}} status has been changed.</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Customer.NewMerchandiseReturnNote",
                Subject = "{{Store.Name}}. New merchandise return note has been added",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Customer.FullName}},<br />\r\nYour merchandise return #{{MerchandiseReturn.ReturnNumber}} has a new note.</p>",
                IsActive = true,
                EmailAccountId = ea
            },
        };
    }
}
