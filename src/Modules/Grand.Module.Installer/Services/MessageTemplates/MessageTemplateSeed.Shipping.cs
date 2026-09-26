using Grand.Domain.Messages;

namespace Grand.Module.Installer.Services.MessageTemplates;

public static partial class MessageTemplateSeed
{
    private static IEnumerable<MessageTemplate> ShippingTemplates(string ea)
    {
        return new List<MessageTemplate> {
            new() {
                Name = "ShipmentSent.CustomerNotification",
                Subject = "{{Store.Name}} — Order #{{Order.OrderNumber}} has shipped",
                Body =
                    Layout(
                        "Your order is on its way.",
                        "Your order is on its way",
                        @"<p>Hello {{Order.CustomerFullName}},</p>
                <p>Good news — your order has shipped. Here are the details.</p>
                <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px; margin: 8px 0 16px 0;'>
                <tr><td style='padding: 4px 0; color: #71717a;'>Order number</td><td style='padding: 4px 0; text-align: right;'><strong>{{Order.OrderNumber}}</strong></td></tr>
                <tr><td style='padding: 4px 0; color: #71717a;'>Date ordered</td><td style='padding: 4px 0; text-align: right;'>{{Order.CreatedOn}}</td></tr>
                <tr><td style='padding: 4px 0; color: #71717a;'>Shipping method</td><td style='padding: 4px 0; text-align: right;'>{{Order.ShippingMethod}}</td></tr>
                {% if Shipment.TrackingNumber != null and Shipment.TrackingNumber != '' %}
                <tr><td style='padding: 4px 0; color: #71717a;'>Tracking number</td><td style='padding: 4px 0; text-align: right;'>{{Shipment.TrackingNumber}}</td></tr>
                {% endif %}
                </table>
                " + Button("{{Order.OrderURLForCustomer}}", "View order") + @"
                <p><strong>Shipped products</strong></p>
                " + ShipmentProducts + @"
                <p><strong>Billing address</strong><br />
                {{Order.BillingFirstName}} {{Order.BillingLastName}}<br />
                {{Order.BillingAddress1}}<br />
                {{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />
                {{Order.BillingStateProvince}} {{Order.BillingCountry}}</p>
                <p><strong>Shipping address</strong><br />
                {{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />
                {{Order.ShippingAddress1}}<br />
                {{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />
                {{Order.ShippingStateProvince}} {{Order.ShippingCountry}}</p>"),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "ShipmentDelivered.CustomerNotification",
                Subject = "{{Store.Name}} — Order #{{Order.OrderNumber}} has been delivered",
                Body =
                    Layout(
                        "Your order has arrived.",
                        "Your order has been delivered",
                        @"<p>Hello {{Order.CustomerFullName}},</p>
                <p>Good news — your order has been delivered. Here are the details.</p>
                <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px; margin: 8px 0 16px 0;'>
                <tr><td style='padding: 4px 0; color: #71717a;'>Order number</td><td style='padding: 4px 0; text-align: right;'><strong>{{Order.OrderNumber}}</strong></td></tr>
                <tr><td style='padding: 4px 0; color: #71717a;'>Date ordered</td><td style='padding: 4px 0; text-align: right;'>{{Order.CreatedOn}}</td></tr>
                <tr><td style='padding: 4px 0; color: #71717a;'>Shipping method</td><td style='padding: 4px 0; text-align: right;'>{{Order.ShippingMethod}}</td></tr>
                </table>
                " + Button("{{Order.OrderURLForCustomer}}", "View order") + @"
                <p><strong>Delivered products</strong></p>
                " + ShipmentProducts + @"
                <p><strong>Billing address</strong><br />
                {{Order.BillingFirstName}} {{Order.BillingLastName}}<br />
                {{Order.BillingAddress1}}<br />
                {{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />
                {{Order.BillingStateProvince}} {{Order.BillingCountry}}</p>
                <p><strong>Shipping address</strong><br />
                {{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />
                {{Order.ShippingAddress1}}<br />
                {{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />
                {{Order.ShippingStateProvince}} {{Order.ShippingCountry}}</p>"),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "NewMerchandiseReturn.CustomerNotification",
                Subject = "{{Store.Name}} — Merchandise return #{{MerchandiseReturn.ReturnNumber}} received",
                Body =
                    Layout(
                        "We've received your return request.",
                        "We've received your return request",
                        @"<p>Hello {{Customer.FullName}},</p>
                <p>We've received your merchandise return request. Here is a summary.</p>
                <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px; margin: 8px 0 16px 0;'>
                <tr><td style='padding: 4px 0; color: #71717a;'>Request ID</td><td style='padding: 4px 0; text-align: right;'><strong>{{MerchandiseReturn.ReturnNumber}}</strong></td></tr>
                <tr><td style='padding: 4px 0; color: #71717a;'>Pickup date</td><td style='padding: 4px 0; text-align: right;'>{{MerchandiseReturn.PickupDate}}</td></tr>
                </table>
                {% if MerchandiseReturn.CustomerComment != null and MerchandiseReturn.CustomerComment != '' %}
                <p><strong>Your comments</strong><br />
                {{MerchandiseReturn.CustomerComment}}</p>
                {% endif %}
                " + Button("{{Store.URL}}merchandisereturndetails/{{MerchandiseReturn.Id}}", "View return") + @"
                <p><strong>Pickup address</strong><br />
                {{MerchandiseReturn.PickupAddressFirstName}} {{MerchandiseReturn.PickupAddressLastName}}<br />
                {{MerchandiseReturn.PickupAddressAddress1}}<br />
                {{MerchandiseReturn.PickupAddressCity}} {{MerchandiseReturn.PickupAddressZipPostalCode}}<br />
                {{MerchandiseReturn.PickupAddressStateProvince}} {{MerchandiseReturn.PickupAddressCountry}}</p>
                <p>We'll be in touch once your return has been reviewed.</p>"),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "NewMerchandiseReturn.StoreOwnerNotification",
                Subject = "{{Store.Name}} — New merchandise return #{{MerchandiseReturn.ReturnNumber}} submitted",
                Body =
                    Layout(
                        "A customer submitted a new merchandise return.",
                        "New merchandise return submitted",
                        @"<p>{{Customer.FullName}} has just submitted a new merchandise return. Details are below.</p>
                <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px; margin: 8px 0 16px 0;'>
                <tr><td style='padding: 4px 0; color: #71717a;'>Request ID</td><td style='padding: 4px 0; text-align: right;'><strong>{{MerchandiseReturn.ReturnNumber}}</strong></td></tr>
                <tr><td style='padding: 4px 0; color: #71717a;'>Pickup date</td><td style='padding: 4px 0; text-align: right;'>{{MerchandiseReturn.PickupDate}}</td></tr>
                </table>
                {% if MerchandiseReturn.CustomerComment != null and MerchandiseReturn.CustomerComment != '' %}
                <p><strong>Customer comments</strong><br />
                {{MerchandiseReturn.CustomerComment}}</p>
                {% endif %}
                <p><strong>Pickup address</strong><br />
                {{MerchandiseReturn.PickupAddressFirstName}} {{MerchandiseReturn.PickupAddressLastName}}<br />
                {{MerchandiseReturn.PickupAddressAddress1}}<br />
                {{MerchandiseReturn.PickupAddressCity}} {{MerchandiseReturn.PickupAddressZipPostalCode}}<br />
                {{MerchandiseReturn.PickupAddressStateProvince}} {{MerchandiseReturn.PickupAddressCountry}}</p>"),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "MerchandiseReturnStatusChanged.CustomerNotification",
                Subject = "{{Store.Name}} — Return #{{MerchandiseReturn.ReturnNumber}} status updated",
                Body =
                    Layout(
                        "Your merchandise return status has changed.",
                        "Your return status has been updated",
                        @"<p>Hello {{Customer.FullName}},</p>
                <p>The status of your merchandise return #{{MerchandiseReturn.ReturnNumber}} has changed to <strong>{{MerchandiseReturn.Status}}</strong>.</p>
                " + Button("{{Store.URL}}merchandisereturndetails/{{MerchandiseReturn.Id}}", "View return") + @"
                <p>If you have any questions about your return, just reply to this email.</p>"),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Customer.NewMerchandiseReturnNote",
                Subject = "{{Store.Name}} — New note on return #{{MerchandiseReturn.ReturnNumber}}",
                Body =
                    Layout(
                        "A new note was added to your return.",
                        "New note on your return",
                        @"<p>Hello {{Customer.FullName}},</p>
                <p>A new note has been added to your merchandise return #{{MerchandiseReturn.ReturnNumber}}.</p>
                {% if MerchandiseReturn.NewNoteText != null and MerchandiseReturn.NewNoteText != '' %}
                <p style='padding: 12px 16px; background-color: #f4f4f5; border-radius: 6px;'>{{MerchandiseReturn.NewNoteText}}</p>
                {% endif %}
                " + Button("{{Store.URL}}merchandisereturndetails/{{MerchandiseReturn.Id}}", "View return")),
                IsActive = true,
                EmailAccountId = ea
            },
        };
    }
}
