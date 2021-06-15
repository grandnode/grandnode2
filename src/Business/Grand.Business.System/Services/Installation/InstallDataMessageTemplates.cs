using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Directory;
using Grand.Domain.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallMessageTemplates()
        {
            var eaGeneral = _emailAccountRepository.Table.FirstOrDefault();
            if (eaGeneral == null)
                throw new Exception("Default email account cannot be loaded");

            var OrderProducts = @"
                <table style='width: 100%;' border='0'>
                <tr style='background-color: #b9babe; text-align: center;'><th>Name</th><th>Price</th><th>Quantity</th><th>Total</th></tr>
                {% for item in Order.OrderItems -%}
                <tr style='background-color: #ebecee; text-align: center;'>
                <td style='padding: 0.6em 0.4em; text-align: left;'>{{item.ProductName}}
                {% if item.IsDownloadAllowed -%}
                <br />
                <a class='link' href='{{item.DownloadUrl}}'>Download</a>
                {% endif %}

                {% if item.IsLicenseDownloadAllowed -%}
                <br />
                <a class='link' href='{{item.LicenseUrl}}'>Download license</a>
                {% endif %}

                {% if item.AttributeDescription != null and item.AttributeDescription != '' %}
                <br />
                {{item.AttributeDescription}}
                {% endif %}

                {% if item.ProductSku != null and item.ProductSku != '' %}
                <br />
                Sku: {{item.ProductSku}}
                {% endif %}

                </td>
                <td style='padding: 0.6em 0.4em; text-align: right;'>{{item.UnitPrice}}</td>
                <td style='padding: 0.6em 0.4em; text-align: center;'>{{item.Quantity}}</td>
                <td style='padding: 0.6em 0.4em; text-align: right;'>{{item.TotalPrice}}</td>
                </tr>
                {% endfor -%}

                {% if Order.CheckoutAttributeDescription != null and Order.CheckoutAttributeDescription != '' %}
                <tr><td style='text-align:right;' colspan='1'>&nbsp;</td><td colspan='3' style='text-align:right'>
                {{Order.CheckoutAttributeDescription}}
                </td></tr>
                {% endif %}

                <tr style='text-align:right;'><td>&nbsp;</td><td colspan='2' style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>Sub-Total:</strong></td> <td style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>{{Order.SubTotal}}</strong></td></tr>

                {% if Order.DisplaySubTotalDiscount %}
                <tr style='text-align:right;'><td>&nbsp;</td><td colspan='2' style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>Discount:</strong></td> <td style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>{{Order.SubTotalDiscount}}</strong></td></tr>
                {% endif %}

                {% if Order.DisplayShipping %}
                <tr style='text-align:right;'><td>&nbsp;</td><td colspan='2' style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>Shipping:</strong></td> <td style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>{{Order.Shipping}}</strong></td></tr>
                {% endif %}

                {% if Order.DisplayPaymentMethodFee %}
                <tr style='text-align:right;'><td>&nbsp;</td><td colspan='2' style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>Payment method additional fee:</strong></td> <td style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>{{Order.PaymentMethodAdditionalFee}}</strong></td></tr>
                {% endif %}

                {% if Order.DisplayTax %}
                <tr style='text-align:right;'><td>&nbsp;</td><td colspan='2' style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>Tax:</strong></td> <td style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>{{Order.Tax}}</strong></td></tr>
                {% endif %}

                {% if Order.DisplayTaxRates %}
                {% for item in Order.TaxRates -%}
                <tr style='text-align:right;'><td>&nbsp;</td><td colspan='2' style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>{{item.Key}}</strong></td> <td style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>{{item.Value}}</strong></td></tr>
                {% endfor -%}
                {% endif %}

                {% if Order.DisplayDiscount %}
                <tr style='text-align:right;'><td>&nbsp;</td><td colspan='2' style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>Discount:</strong></td> <td style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>{{Order.Discount}}</strong></td></tr>
                {% endif %}

                {% for item in Order.GiftVouchers -%}
                <tr style='text-align:right;'><td>&nbsp;</td><td colspan='2' style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>{{item.Key}}</strong></td> <td style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>{{item.Value}}</strong></td></tr>
                {% endfor -%}

                {% if Order.RedeemedLoyaltyPointsEntryExists %}
                <tr style='text-align:right;'><td>&nbsp;</td><td colspan='2' style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>{{Order.RPTitle}}</strong></td> <td style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>{{Order.RPAmount}}</strong></td></tr>
                {% endif %}

                <tr style='text-align:right;'><td>&nbsp;</td><td colspan='2' style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>Order Total:</strong></td> <td style='background-color: #dde2e6;padding:0.6em 0.4 em;'><strong>{{Order.Total}}</strong></td></tr>
                </table>";

            var OrderVendorProducts = @"
                <table style='width: 100%;' border='0'>
                <tr style='background-color: #b9babe; text-align: center;'><th>Name</th><th>Price</th><th>Quantity</th><th>Total</th></tr>
                {% for item in Order.OrderItems -%}
                <tr style='background-color: #ebecee; text-align: center;'>
                <td style='padding: 0.6em 0.4em; text-align: left;'>{{item.ProductName}}
                {% if item.IsDownloadAllowed -%}
                <br />
                <a class='link' href='{{item.DownloadUrl}}'>Download</a>
                {% endif %}

                {% if item.IsLicenseDownloadAllowed -%}
                <br />
                <a class='link' href='{{item.LicenseUrl}}'>Download license</a>
                {% endif %}

                {% if item.AttributeDescription != null and item.AttributeDescription != '' %}
                <br />
                {{item.AttributeDescription}}
                {% endif %}

                {% if item.ProductSku != null and item.ProductSku != '' %}
                <br />
                Sku: {{item.ProductSku}}
                {% endif %}

                </td>
                <td style='padding: 0.6em 0.4em; text-align: right;'>{{item.UnitPrice}}</td>
                <td style='padding: 0.6em 0.4em; text-align: center;'>{{item.Quantity}}</td>
                <td style='padding: 0.6em 0.4em; text-align: right;'>{{item.TotalPrice}}</td>
                </tr>
                {% endfor -%}

                {% if Order.CheckoutAttributeDescription != null and Order.CheckoutAttributeDescription != '' %}
                <tr><td style='text-align:right;' colspan='1'>&nbsp;</td><td colspan='3' style='text-align:right'>
                {{Order.CheckoutAttributeDescription}}
                </td></tr>
                {% endif %}
                </table>
                ";

            var ShipmentProducts = @"
                <table border='0' style='width:100%;'>
                <tr style='background-color:#b9babe; text-align:center;'>
                <th>Name</th>
                <th>Quantity</th>
                </tr>

                {% for item in Shipment.ShipmentItems -%}
                <tr style='background-color: #ebecee; text-align: center;'>
                <td style='padding: 0.6em 0.4em;text-align: left;'>{{item.ProductName}}
                {% if item.AttributeDescription != null and item.AttributeDescription != '' %}
                <br />
                {{item.AttributeDescription}}
                {% endif %}

                {% if item.ProductSku != null and item.ProductSku != '' %}
                <br />
                Sku: {{item.ProductSku}}
                {% endif %}

                </td>
                <td style='padding: 0.6em 0.4em; text-align: center;'>{{item.Quantity}}</td>
                </tr>
                {% endfor -%}
                </table>
                ";

            var messageTemplates = new List<MessageTemplate>
                               {
                                    new MessageTemplate
                                       {
                                           Name = "AuctionEnded.CustomerNotificationWin",
                                           Subject = "{{Store.Name}}. Auction ended.",
                                           Body = "<p>Hello, {{Customer.FullName}}!</p><p></p><p>At {{Auctions.EndTime}} you have won <a href=\"{{Store.URL}}{{Auctions.ProductSeName}}\">{{Auctions.ProductName}}</a> for {{Auctions.Price}}. Visit  <a href=\"{{Store.URL}}/cart\">cart</a> to finish checkout process. </p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                    new MessageTemplate
                                            {
                                                Name = "AuctionEnded.CustomerNotificationLost",
                                                Subject = "{{Store.Name}}. Auction ended.",
                                                Body = "<p>Hello, {{Customer.FullName}}!</p><p></p><p>Unfortunately you did not win the bid {{Auctions.ProductName}}</p> <p>End price:  {{Auctions.Price}} </p> <p>End date auction {{Auctions.EndTime}} </p>",
                                                IsActive = true,
                                                EmailAccountId = eaGeneral.Id,
                                            },
                                    new MessageTemplate
                                            {
                                                Name = "AuctionEnded.CustomerNotificationBin",
                                                Subject = "{{Store.Name}}. Auction ended.",
                                                Body = "<p>Hello, {{Customer.FullName}}!</p><p></p><p>Unfortunately you did not win the bid {{Product.Name}}</p> <p>Product was bought by option Buy it now for price: {{Product.Price}} </p>",
                                                IsActive = true,
                                                EmailAccountId = eaGeneral.Id,
                                            },
                                    new MessageTemplate
                                       {
                                           Name = "AuctionEnded.StoreOwnerNotification",
                                           Subject = "{{Store.Name}}. Auction ended.",
                                           Body = "<p>At {{Auctions.EndTime}} {{Customer.FullName}} have won <a href=\"{{Store.URL}}{{Auctions.ProductSeName}}\">{{Auctions.ProductName}}</a> for {{Auctions.Price}}.</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                    new MessageTemplate
                                       {
                                           Name = "AuctionExpired.StoreOwnerNotification",
                                           Subject = "Your auction to product {{Product.Name}}  has expired.",
                                           Body = "Hello, <br> Your auction to product {{Product.Name}} has expired without bid.",
                                           IsActive = false,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                    new MessageTemplate
                                       {
                                           Name = "BidUp.CustomerNotification",
                                           Subject = "{{Store.Name}}. Your offer has been outbid.",
                                           Body = "<p>Hi {{Customer.FullName}}!</p><p>Your offer for product <a href=\"{{Store.URL}}{{Auctions.ProductSeName}}\">{{Auctions.ProductName}}</a> has been outbid. Your price was {{Auctions.Price}}.<br />\r\nRaise a price by raising one's offer. Auction will be ended on {{Auctions.EndTime}}</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                    new MessageTemplate
                                       {
                                           Name = "Blog.BlogComment",
                                           Subject = "{{Store.Name}}. New blog comment.",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nA new blog comment has been created for blog post \"{{BlogComment.BlogPostTitle}}\".</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "Knowledgebase.ArticleComment",
                                           Subject = "{{Store.Name}}. New article comment.",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nA new article comment has been created for article \"{{Knowledgebase.ArticleCommentTitle}}\".</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "Customer.OutOfStock",
                                           Subject = "{{Store.Name}}. Back in stock notification",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Customer.FullName}}, <br />\r\nProduct <a target=\"_blank\" href=\"{{OutOfStockSubscription.ProductUrl}}\">{{OutOfStockSubscription.ProductName}}</a> is in stock.</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "CustomerDelete.StoreOwnerNotification",
                                           Subject = "{{Store.Name}}. Customer has been deleted.",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> ,<br />\r\n{{Customer.FullName}} ({{Customer.Email}}) has just deleted from your database. </p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "Customer.EmailTokenValidationMessage",
                                           Subject = "{{Store.Name}} - Email Verification Code",
                                           Body = "Hello {{Customer.FullName}}, <br /><br />\r\n Enter this 6 digit code on the sign in page to confirm your identity:<br /><br /> \r\n <b>{{Customer.Token}}</b><br /><br />\r\n Yours securely, <br /> \r\n Team",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "Customer.EmailValidationMessage",
                                           Subject = "{{Store.Name}}. Email validation",
                                           Body = "<a href=\"{{Store.URL}}\">{{Store.Name}}</a>  <br />\r\n  <br />\r\n  To activate your account <a href=\"{{Customer.AccountActivationURL}}\">click here</a>.     <br />\r\n  <br />\r\n  {{Store.Name}}",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "Customer.NewPM",
                                           Subject = "{{Store.Name}}. You have received a new private message",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nYou have received a new private message.</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "Customer.PasswordRecovery",
                                           Subject = "{{Store.Name}}. Password recovery",
                                           Body = "<a href=\"{{Store.URL}}\">{{Store.Name}}</a>  <br />\r\n  <br />\r\n  To change your password <a href=\"{{Customer.PasswordRecoveryURL}}\">click here</a>.     <br />\r\n  <br />\r\n  {{Store.Name}}",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "Customer.WelcomeMessage",
                                           Subject = "Welcome to {{Store.Name}}",
                                           Body = "We welcome you to <a href=\"{{Store.URL}}\"> {{Store.Name}}</a>.<br />\r\n<br />\r\nYou can now take part in the various services we have to offer you. Some of these services include:<br />\r\n<br />\r\nPermanent Cart - Any products added to your online cart remain there until you remove them, or check them out.<br />\r\nAddress Book - We can now deliver your products to another address other than yours! This is perfect to send birthday gifts direct to the birthday-person themselves.<br />\r\nOrder History - View your history of purchases that you have made with us.<br />\r\nProducts Reviews - Share your opinions on products with our other customers.<br />\r\n<br />\r\nFor help with any of our online services, please email the store-owner: <a href=\"mailto:{{Store.Email}}\">{{Store.Email}}</a>.<br />\r\n<br />\r\nNote: This email address was provided on our registration page. If you own the email and did not register on our site, please send an email to <a href=\"mailto:{{Store.Email}}\">{{Store.Email}}</a>.",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "GiftVoucher.Notification",
                                           Subject = "{{GiftVoucher.SenderName}} has sent you a gift voucher for {{Store.Name}}",
                                           Body = "<p>You have received a gift voucher for {{Store.Name}}</p><p>Dear {{GiftVoucher.RecipientName}}, <br />\r\n<br />\r\n{{GiftVoucher.SenderName}} ({{GiftVoucher.SenderEmail}}) has sent you a {{GiftVoucher.Amount}} gift cart for <a href=\"{{Store.URL}}\"> {{Store.Name}}</a></p><p>You gift voucher code is {{GiftVoucher.CouponCode}}</p><p>{{GiftVoucher.Message}}</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "NewCustomer.Notification",
                                           Subject = "{{Store.Name}}. New customer registration",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nA new customer registered with your store. Below are the customer's details:<br />\r\nFull name: {{Customer.FullName}}<br />\r\nEmail: {{Customer.Email}}</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "NewMerchandiseReturn.CustomerNotification",
                                           Subject = "{{Store.Name}}. New merchandise return.",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Customer.FullName}}!<br />\r\n You have just submitted a new merchandise return. Details are below:<br />\r\nRequest ID: {{MerchandiseReturn.ReturnNumber}}<br />\r\nCustomer comments: {{MerchandiseReturn.CustomerComment}}<br />\r\n<br />\r\nPickup date: {{MerchandiseReturn.PickupDate}}<br />\r\n<br />\r\nPickup address:<br />\r\n{{MerchandiseReturn.PickupAddressFirstName}} {{MerchandiseReturn.PickupAddressLastName}}<br />\r\n{{MerchandiseReturn.PickupAddressAddress1}}<br />\r\n{{MerchandiseReturn.PickupAddressCity}} {{MerchandiseReturn.PickupAddressZipPostalCode}}<br />\r\n{{MerchandiseReturn.PickupAddressStateProvince}} {{MerchandiseReturn.PickupAddressCountry}}<br />\r\n</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "NewMerchandiseReturn.StoreOwnerNotification",
                                           Subject = "{{Store.Name}}. New merchandise return.",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\n{{Customer.FullName}} has just submitted a new merchandise return. Details are below:<br />\r\nRequest ID: {{MerchandiseReturn.ReturnNumber}}<br />\r\nCustomer comments: {{MerchandiseReturn.CustomerComment}}<br />\r\n<br />\r\nPickup date: {{MerchandiseReturn.PickupDate}}<br />\r\n<br />\r\nPickup address:<br />\r\n{{MerchandiseReturn.PickupAddressFirstName}} {{MerchandiseReturn.PickupAddressLastName}}<br />\r\n{{MerchandiseReturn.PickupAddressAddress1}}<br />\r\n{{MerchandiseReturn.PickupAddressCity}} {{MerchandiseReturn.PickupAddressZipPostalCode}}<br />\r\n{{MerchandiseReturn.PickupAddressStateProvince}} {{MerchandiseReturn.PickupAddressCountry}}<br />\r\n</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "News.NewsComment",
                                           Subject = "{{Store.Name}}. New news comment.",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nA new news comment has been created for news \"{{NewsComment.NewsTitle}}\".</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "NewsLetterSubscription.ActivationMessage",
                                           Subject = "{{Store.Name}}. Subscription activation message.",
                                           Body = "<p><a href=\"{{NewsLetterSubscription.ActivationUrl}}\">Click here to confirm your subscription to our list.</a></p><p>If you received this email by mistake, simply delete it.</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "NewsLetterSubscription.DeactivationMessage",
                                           Subject = "{{Store.Name}}. Subscription deactivation message.",
                                           Body = "<p><a href=\"{{NewsLetterSubscription.DeactivationUrl}}\">Click here to unsubscribe from our newsletter.</a></p><p>If you received this email by mistake, simply delete it.</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                  new MessageTemplate
                                       {
                                           Name = "OrderCancelled.StoreOwnerNotification",
                                           Subject = "{{Store.Name}}. Customer cancelled an order",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\n<br />\r\nCustomer cancelled an order. Below is the summary of the order. <br />\r\n<br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nOrder Details: <a target=\"_blank\" href=\"{{Order.OrderURLForCustomer}}\">{{Order.OrderURLForCustomer}}</a><br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nBilling Address<br />\r\n{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />\r\n{{Order.BillingAddress1}}<br />\r\n{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />\r\n{{Order.BillingStateProvince}} {{Order.BillingCountry}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nShipping Address<br />\r\n{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />\r\n{{Order.ShippingAddress1}}<br />\r\n{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />\r\n{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}<br />\r\n<br />\r\nShipping Method: {{Order.ShippingMethod}}<br />\r\n<br />\r\n" + OrderProducts + "</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                    new MessageTemplate
                                       {
                                           Name = "OrderCancelled.CustomerNotification",
                                           Subject = "{{Store.Name}}. Your order cancelled",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Order.CustomerFullName}}, <br />\r\nYour order has been cancelled. Below is the summary of the order. <br />\r\n<br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nOrder Details: <a target=\"_blank\" href=\"{{Order.OrderURLForCustomer}}\">{{Order.OrderURLForCustomer}}</a><br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nBilling Address<br />\r\n{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />\r\n{{Order.BillingAddress1}}<br />\r\n{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />\r\n{{Order.BillingStateProvince}} {{Order.BillingCountry}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nShipping Address<br />\r\n{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />\r\n{{Order.ShippingAddress1}}<br />\r\n{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />\r\n{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}<br />\r\n<br />\r\nShipping Method: {{Order.ShippingMethod}}<br />\r\n<br />\r\n" + OrderProducts + "</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                    new MessageTemplate
                                       {
                                           Name = "OrderCancelled.VendorNotification",
                                           Subject = "{{Store.Name}}. Order #{{Order.OrderNumber}} cancelled",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br /><br />Order #{{Order.OrderNumber}} has been cancelled. <br /><br />Order Number: {{Order.OrderNumber}} <br />   Date Ordered: {{Order.CreatedOn}} <br /><br /> ",
                                           IsActive = false,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                    new MessageTemplate
                                       {
                                           Name = "OrderCompleted.CustomerNotification",
                                           Subject = "{{Store.Name}}. Your order completed",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Order.CustomerFullName}}, <br />\r\nYour order has been completed. Below is the summary of the order. <br />\r\n<br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nOrder Details: <a target=\"_blank\" href=\"{{Order.OrderURLForCustomer}}\">{{Order.OrderURLForCustomer}}</a><br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nBilling Address<br />\r\n{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />\r\n{{Order.BillingAddress1}}<br />\r\n{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />\r\n{{Order.BillingStateProvince}} {{Order.BillingCountry}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nShipping Address<br />\r\n{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />\r\n{{Order.ShippingAddress1}}<br />\r\n{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />\r\n{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}<br />\r\n<br />\r\nShipping Method: {{Order.ShippingMethod}}<br />\r\n<br />\r\n" + OrderProducts + "</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "ShipmentDelivered.CustomerNotification",
                                           Subject = "Your order from {{Store.Name}} has been delivered.",
                                           Body = "<p><a href=\"{{Store.URL}}\"> {{Store.Name}}</a> <br />\r\n <br />\r\n Hello {{Order.CustomerFullName}}, <br />\r\n Good news! You order has been delivered. <br />\r\n Order Number: {{Order.OrderNumber}}<br />\r\n Order Details: <a href=\"{{Order.OrderURLForCustomer}}\" target=\"_blank\">{{Order.OrderURLForCustomer}}</a><br />\r\n Date Ordered: {{Order.CreatedOn}}<br />\r\n <br />\r\n <br />\r\n <br />\r\n Billing Address<br />\r\n {{Order.BillingFirstName}} {{Order.BillingLastName}}<br />\r\n {{Order.BillingAddress1}}<br />\r\n {{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />\r\n {{Order.BillingStateProvince}} {{Order.BillingCountry}}<br />\r\n <br />\r\n <br />\r\n <br />\r\n Shipping Address<br />\r\n {{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />\r\n {{Order.ShippingAddress1}}<br />\r\n {{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />\r\n {{Order.ShippingStateProvince}} {{Order.ShippingCountry}}<br />\r\n <br />\r\n Shipping Method: {{Order.ShippingMethod}} <br />\r\n <br />\r\n Delivered Products: <br />\r\n <br />\r\n" + ShipmentProducts + "</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "OrderPlaced.CustomerNotification",
                                           Subject = "Order receipt from {{Store.Name}}.",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Order.CustomerFullName}}, <br />\r\nThanks for buying from <a href=\"{{Store.URL}}\">{{Store.Name}}</a>. Below is the summary of the order. <br />\r\n<br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nOrder Details: <a target=\"_blank\" href=\"{{Order.OrderURLForCustomer}}\">{{Order.OrderURLForCustomer}}</a><br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nBilling Address<br />\r\n{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />\r\n{{Order.BillingAddress1}}<br />\r\n{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />\r\n{{Order.BillingStateProvince}} {{Order.BillingCountry}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nShipping Address<br />\r\n{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />\r\n{{Order.ShippingAddress1}}<br />\r\n{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />\r\n{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}<br />\r\n<br />\r\nShipping Method: {{Order.ShippingMethod}}<br />\r\n<br />\r\n" + OrderProducts + "</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "OrderPlaced.StoreOwnerNotification",
                                           Subject = "{{Store.Name}}. Purchase Receipt for Order #{{Order.OrderNumber}}",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\n{{Order.CustomerFullName}} ({{Order.CustomerEmail}}) has just placed an order from your store. Below is the summary of the order. <br />\r\n<br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nBilling Address<br />\r\n{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />\r\n{{Order.BillingAddress1}}<br />\r\n{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />\r\n{{Order.BillingStateProvince}} {{Order.BillingCountry}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nShipping Address<br />\r\n{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />\r\n{{Order.ShippingAddress1}}<br />\r\n{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />\r\n{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}<br />\r\n<br />\r\nShipping Method: {{Order.ShippingMethod}}<br />\r\n<br />\r\n" + OrderProducts + "</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "ShipmentSent.CustomerNotification",
                                           Subject = "Your order from {{Store.Name}} has been shipped.",
                                           Body = "<p><a href=\"{{Store.URL}}\"> {{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Order.CustomerFullName}}!, <br />\r\nGood news! You order has been shipped. <br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nOrder Details: <a href=\"{{Order.OrderURLForCustomer}}\" target=\"_blank\">{{Order.OrderURLForCustomer}}</a><br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nBilling Address<br />\r\n{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />\r\n{{Order.BillingAddress1}}<br />\r\n{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />\r\n{{Order.BillingStateProvince}} {{Order.BillingCountry}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nShipping Address<br />\r\n{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />\r\n{{Order.ShippingAddress1}}<br />\r\n{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />\r\n{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}<br />\r\n<br />\r\nShipping Method: {{Order.ShippingMethod}} <br />\r\n <br />\r\n Shipped Products: <br />\r\n <br />\r\n" + ShipmentProducts + "</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "Product.ProductReview",
                                           Subject = "{{Store.Name}}. New product review.",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nA new product review has been written for product \"{{ProductReview.ProductName}}\".</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "QuantityBelow.StoreOwnerNotification",
                                           Subject = "{{Store.Name}}. Quantity below notification. {{Product.Name}}",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\n{{Product.Name}} (ID: {{Product.Id}}) low quantity. <br />\r\n<br />\r\nQuantity: {{Product.StockQuantity}}<br />\r\n</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "QuantityBelow.AttributeCombination.StoreOwnerNotification",
                                           Subject = "{{Store.Name}}. Quantity below notification. {{Product.Name}}",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\n{{Product.Name}} (ID: {{Product.Id}}) low quantity. <br />\r\n{{AttributeCombination.Formatted}}<br />\r\nQuantity: {{AttributeCombination.StockQuantity}}<br />\r\n</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "MerchandiseReturnStatusChanged.CustomerNotification",
                                           Subject = "{{Store.Name}}. Merchandise return status was changed.",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Customer.FullName}},<br />\r\nYour merchandise return #{{MerchandiseReturn.ReturnNumber}} status has been changed.</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "Service.EmailAFriend",
                                           Subject = "{{Store.Name}}. Referred Item",
                                           Body = "<p><a href=\"{{Store.URL}}\"> {{Store.Name}}</a> <br />\r\n<br />\r\n{{EmailAFriend.Email}} was shopping on {{Store.Name}} and wanted to share the following item with you. <br />\r\n<br />\r\n<b><a target=\"_blank\" href=\"{{Product.ProductURLForCustomer}}\">{{Product.Name}}</a></b> <br />\r\n{{Product.ShortDescription}} <br />\r\n<br />\r\nFor more info click <a target=\"_blank\" href=\"{{Product.ProductURLForCustomer}}\">here</a> <br />\r\n<br />\r\n<br />\r\n{{EmailAFriend.PersonalMessage}}<br />\r\n<br />\r\n{{Store.Name}}</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "Service.AskQuestion",
                                           Subject = "{{Store.Name}}. Question about a product",
                                           Body = "<p><a href=\"{{Store.URL}}\"> {{Store.Name}}</a> <br />\r\n<br />\r\n{{AskQuestion.Email}} wanted to ask question about a product {{Product.Name}}. <br />\r\n<br />\r\n<b><a target=\"_blank\" href=\"{{Product.ProductURLForCustomer}}\">{{Product.Name}}</a></b> <br />\r\n{{Product.ShortDescription}} <br />\r\n{{AskQuestion.Message}}<br />\r\n {{AskQuestion.Email}} <br />\r\n {{AskQuestion.FullName}} <br />\r\n {{AskQuestion.Phone}} <br />\r\n{{Store.Name}}</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "Service.ContactUs",
                                           Subject = "{{Store.Name}}. Contact us",
                                           Body = "<p>From {{ContactUs.SenderName}} - {{ContactUs.SenderEmail}}<br /><br />{{ContactUs.Body}}<br />{{ContactUs.AttributeDescription}}</p><br />",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "Service.ContactVendor",
                                           Subject = "{{Store.Name}}. Contact us",
                                           Body = "<p>From {{ContactUs.SenderName}} - {{ContactUs.SenderEmail}}<br /><br />{{ContactUs.Body}}</p><br />",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },

                                   new MessageTemplate
                                       {
                                           Name = "Wishlist.EmailAFriend",
                                           Subject = "{{Store.Name}}. Wishlist",
                                           Body = "<p><a href=\"{{Store.URL}}\"> {{Store.Name}}</a> <br />\r\n<br />\r\n{{EmailAFriend.Email}} was shopping on {{Store.Name}} and wanted to share a wishlist with you <br />\r\n<br />\r\n<br />\r\nFor more info click <a target=\"_blank\" href=\"{{Customer.WishlistURLForCustomer}}\">here</a> <br />\r\n<br />\r\n<br />\r\n{{EmailAFriend.PersonalMessage}}<br />\r\n<br />\r\n{{Store.Name}}</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "Customer.NewOrderNote",
                                           Subject = "{{Store.Name}}. New order note has been added",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Customer.FullName}}, <br />\r\nNew order note has been added to your account:<br />\r\n\"{{Order.NewNoteText}}\".<br />\r\n<a target=\"_blank\" href=\"{{Order.OrderURLForCustomer}}\">{{Order.OrderURLForCustomer}}</a></p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "Customer.NewCustomerNote",
                                           Subject = "New customer note has been added",
                                           Body = "<p><br />\r\nHello {{Customer.FullName}}, <br />\r\nNew customer note has been added to your account:<br />\r\n\"{{Customer.NewTitleText}}\".<br />\r\n</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                     new MessageTemplate
                                       {
                                           Name = "Customer.NewMerchandiseReturnNote",
                                           Subject = "{{Store.Name}}. New merchandise return note has been added",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Customer.FullName}},<br />\r\nYour merchandise return #{{MerchandiseReturn.ReturnNumber}} has a new note.</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "OrderPlaced.VendorNotification",
                                           Subject = "{{Store.Name}}. Order placed",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\n{{Customer.FullName}} ({{Customer.Email}}) has just placed an order. <br />\r\n<br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n" + OrderVendorProducts + "</p>",
                                           //this template is disabled by default
                                           IsActive = false,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "OrderPaid.StoreOwnerNotification",
                                           Subject = "{{Store.Name}}. Order #{{Order.OrderNumber}} paid",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nOrder #{{Order.OrderNumber}} has been just paid<br />\r\nDate Ordered: {{Order.CreatedOn}}</p>",
                                           //this template is disabled by default
                                           IsActive = false,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "OrderPaid.CustomerNotification",
                                           Subject = "{{Store.Name}}. Order #{{Order.OrderNumber}} paid",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Order.CustomerFullName}}, <br />\r\nThanks for buying from <a href=\"{{Store.URL}}\">{{Store.Name}}</a>. Order #{{Order.OrderNumber}} has been just paid. Below is the summary of the order. <br />\r\n<br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nOrder Details: <a href=\"{{Order.OrderURLForCustomer}}\" target=\"_blank\">{{Order.OrderURLForCustomer}}</a><br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nBilling Address<br />\r\n{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />\r\n{{Order.BillingAddress1}}<br />\r\n{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />\r\n{{Order.BillingStateProvince}} {{Order.BillingCountry}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nShipping Address<br />\r\n{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />\r\n{{Order.ShippingAddress1}}<br />\r\n{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />\r\n{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}<br />\r\n<br />\r\nShipping Method: {{Order.ShippingMethod}}<br />\r\n<br />\r\n" + OrderProducts + "</p>",
                                           //this template is disabled by default
                                           IsActive = false,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                       {
                                           Name = "OrderPaid.VendorNotification",
                                           Subject = "{{Store.Name}}. Order #{{Order.OrderNumber}} paid",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nOrder #{{Order.OrderNumber}} has been just paid. <br />\r\n<br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n" + OrderVendorProducts + "</p>",
                                           //this template is disabled by default
                                           IsActive = false,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                   new MessageTemplate
                                        {
                                           Name = "OrderRefunded.CustomerNotification",
                                           Subject = "{{Store.Name}}. Order #{{Order.OrderNumber}} refunded",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nHello {{Order.CustomerFullName}}, <br />\r\nThanks for buying from <a href=\"{{Store.URL}}\">{{Store.Name}}</a>. Order #{{Order.OrderNumber}} has been has been refunded. Please allow 7-14 days for the refund to be reflected in your account.<br />\r\n<br />\r\nAmount refunded: {{Order.AmountRefunded}}<br />\r\n<br />\r\nBelow is the summary of the order. <br />\r\n<br />\r\nOrder Number: {{Order.OrderNumber}}<br />\r\nOrder Details: <a href=\"{{Order.OrderURLForCustomer}}\" target=\"_blank\">{{Order.OrderURLForCustomer}}</a><br />\r\nDate Ordered: {{Order.CreatedOn}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nBilling Address<br />\r\n{{Order.BillingFirstName}} {{Order.BillingLastName}}<br />\r\n{{Order.BillingAddress1}}<br />\r\n{{Order.BillingCity}} {{Order.BillingZipPostalCode}}<br />\r\n{{Order.BillingStateProvince}} {{Order.BillingCountry}}<br />\r\n<br />\r\n<br />\r\n<br />\r\nShipping Address<br />\r\n{{Order.ShippingFirstName}} {{Order.ShippingLastName}}<br />\r\n{{Order.ShippingAddress1}}<br />\r\n{{Order.ShippingCity}} {{Order.ShippingZipPostalCode}}<br />\r\n{{Order.ShippingStateProvince}} {{Order.ShippingCountry}}<br />\r\n<br />\r\nShipping Method: {{Order.ShippingMethod}}<br />\r\n<br />\r\n" + OrderProducts + "</p>",
                                           //this template is disabled by default
                                           IsActive = false,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                    new MessageTemplate
                                        {
                                           Name = "OrderRefunded.StoreOwnerNotification",
                                           Subject = "{{Store.Name}}. Order #{{Order.OrderNumber}} refunded",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nOrder #{{Order.OrderNumber}} has been just refunded<br />\r\n<br />\r\nAmount refunded: {{Order.AmountRefunded}}<br />\r\n<br />\r\nDate Ordered: {{Order.CreatedOn}}</p>",
                                           //this template is disabled by default
                                           IsActive = false,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                    new MessageTemplate
                                       {
                                           Name = "VendorAccountApply.StoreOwnerNotification",
                                           Subject = "{{Store.Name}}. New vendor account submitted.",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\n{{Customer.FullName}} ({{Customer.Email}}) has just submitted for a vendor account. Details are below:<br />\r\nVendor name: {{Vendor.Name}}<br />\r\nVendor email: {{Vendor.Email}}<br />\r\n<br />\r\nYou can activate it in admin area.</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                    new MessageTemplate
                                       {
                                           Name = "Vendor.VendorReview",
                                           Subject = "{{Store.Name}}. New vendor review.",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nA new vendor review has been written.</p>",
                                           IsActive = true,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                                    new MessageTemplate
                                       {
                                           Name = "VendorInformationChange.StoreOwnerNotification",
                                           Subject = "{{Store.Name}}. Vendor {{Vendor.Name}} changed provided information",
                                           Body = "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\n{{Vendor.Name}} changed provided information.</p>",
                                           IsActive = false,
                                           EmailAccountId = eaGeneral.Id,
                                       },
                               };
            await _messageTemplateRepository.InsertAsync(messageTemplates);
        }
    }
}
