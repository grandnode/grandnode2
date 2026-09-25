using Grand.Domain.Messages;

namespace Grand.Module.Installer.Services.MessageTemplates;

public static partial class MessageTemplateSeed
{
    public static List<MessageTemplate> Build(string emailAccountId)
    {
        var templates = new List<MessageTemplate>();
        templates.AddRange(OrdersTemplates(emailAccountId));
        templates.AddRange(ShippingTemplates(emailAccountId));
        templates.AddRange(AccountTemplates(emailAccountId));
        templates.AddRange(AuctionTemplates(emailAccountId));
        templates.AddRange(OwnerNotificationTemplates(emailAccountId));
        templates.AddRange(ServiceTemplates(emailAccountId));
        return templates;
    }

    internal static readonly string OrderProducts = @"
                <table style='width: 100%; border-collapse: collapse; font-size: 14px;' border='0'>
                <tr style='background-color: #f4f4f5;'><th style='padding: 0.6em 0.4em; text-align: left;'>Name</th><th style='padding: 0.6em 0.4em; text-align: right;'>Price</th><th style='padding: 0.6em 0.4em; text-align: center;'>Quantity</th><th style='padding: 0.6em 0.4em; text-align: right;'>Total</th></tr>
                {% for item in Order.OrderItems -%}
                <tr style='border-bottom: 1px solid #e4e4e7;'>
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

                <tr style='border-bottom: 1px solid #e4e4e7;'><td>&nbsp;</td><td colspan='2' style='padding: 0.6em 0.4em; text-align: right;'><strong>Sub-Total:</strong></td> <td style='padding: 0.6em 0.4em; text-align: right;'><strong>{{Order.SubTotal}}</strong></td></tr>

                {% if Order.DisplaySubTotalDiscount %}
                <tr style='border-bottom: 1px solid #e4e4e7;'><td>&nbsp;</td><td colspan='2' style='padding: 0.6em 0.4em; text-align: right;'><strong>Discount:</strong></td> <td style='padding: 0.6em 0.4em; text-align: right;'><strong>{{Order.SubTotalDiscount}}</strong></td></tr>
                {% endif %}

                {% if Order.DisplayShipping %}
                <tr style='border-bottom: 1px solid #e4e4e7;'><td>&nbsp;</td><td colspan='2' style='padding: 0.6em 0.4em; text-align: right;'><strong>Shipping:</strong></td> <td style='padding: 0.6em 0.4em; text-align: right;'><strong>{{Order.Shipping}}</strong></td></tr>
                {% endif %}

                {% if Order.DisplayPaymentMethodFee %}
                <tr style='border-bottom: 1px solid #e4e4e7;'><td>&nbsp;</td><td colspan='2' style='padding: 0.6em 0.4em; text-align: right;'><strong>Payment method additional fee:</strong></td> <td style='padding: 0.6em 0.4em; text-align: right;'><strong>{{Order.PaymentMethodAdditionalFee}}</strong></td></tr>
                {% endif %}

                {% if Order.DisplayTax %}
                <tr style='border-bottom: 1px solid #e4e4e7;'><td>&nbsp;</td><td colspan='2' style='padding: 0.6em 0.4em; text-align: right;'><strong>Tax:</strong></td> <td style='padding: 0.6em 0.4em; text-align: right;'><strong>{{Order.Tax}}</strong></td></tr>
                {% endif %}

                {% if Order.DisplayTaxRates %}
                {% for item in Order.TaxRates -%}
                <tr style='border-bottom: 1px solid #e4e4e7;'><td>&nbsp;</td><td colspan='2' style='padding: 0.6em 0.4em; text-align: right;'><strong>{{item.Key}}</strong></td> <td style='padding: 0.6em 0.4em; text-align: right;'><strong>{{item.Value}}</strong></td></tr>
                {% endfor -%}
                {% endif %}

                {% if Order.DisplayDiscount %}
                <tr style='border-bottom: 1px solid #e4e4e7;'><td>&nbsp;</td><td colspan='2' style='padding: 0.6em 0.4em; text-align: right;'><strong>Discount:</strong></td> <td style='padding: 0.6em 0.4em; text-align: right;'><strong>{{Order.Discount}}</strong></td></tr>
                {% endif %}

                {% for item in Order.GiftVouchers -%}
                <tr style='border-bottom: 1px solid #e4e4e7;'><td>&nbsp;</td><td colspan='2' style='padding: 0.6em 0.4em; text-align: right;'><strong>{{item.Key}}</strong></td> <td style='padding: 0.6em 0.4em; text-align: right;'><strong>{{item.Value}}</strong></td></tr>
                {% endfor -%}

                {% if Order.RedeemedLoyaltyPointsEntryExists %}
                <tr style='border-bottom: 1px solid #e4e4e7;'><td>&nbsp;</td><td colspan='2' style='padding: 0.6em 0.4em; text-align: right;'><strong>{{Order.RPTitle}}</strong></td> <td style='padding: 0.6em 0.4em; text-align: right;'><strong>{{Order.RPAmount}}</strong></td></tr>
                {% endif %}

                <tr><td>&nbsp;</td><td colspan='2' style='padding: 0.6em 0.4em; text-align: right;'><strong>Order Total:</strong></td> <td style='padding: 0.6em 0.4em; text-align: right;'><strong>{{Order.Total}}</strong></td></tr>
                </table>";

    internal static readonly string OrderVendorProducts = @"
                <table style='width: 100%; border-collapse: collapse; font-size: 14px;' border='0'>
                <tr style='background-color: #f4f4f5;'><th style='padding: 0.6em 0.4em; text-align: left;'>Name</th><th style='padding: 0.6em 0.4em; text-align: right;'>Price</th><th style='padding: 0.6em 0.4em; text-align: center;'>Quantity</th><th style='padding: 0.6em 0.4em; text-align: right;'>Total</th></tr>
                {% for item in Order.OrderItems -%}
                <tr style='border-bottom: 1px solid #e4e4e7;'>
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

    internal static readonly string ShipmentProducts = @"
                <table border='0' style='width:100%; border-collapse: collapse; font-size: 14px;'>
                <tr style='background-color: #f4f4f5;'>
                <th style='padding: 0.6em 0.4em; text-align: left;'>Name</th>
                <th style='padding: 0.6em 0.4em; text-align: center;'>Quantity</th>
                </tr>

                {% for item in Shipment.ShipmentItems -%}
                <tr style='border-bottom: 1px solid #e4e4e7;'>
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

    internal static string Layout(string preheader, string heading, string content) => $@"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8' />
<meta name='viewport' content='width=device-width, initial-scale=1.0' />
<title>{{{{Store.Name}}}}</title>
</head>
<body style='margin: 0; padding: 0; background-color: #f4f4f5;'>
<div style='display:none;max-height:0;overflow:hidden;'>{preheader}</div>
<table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='width: 100%; background-color: #f4f4f5;'>
<tr>
<td align='center' style='padding: 24px 16px;'>
<table role='presentation' width='600' cellpadding='0' cellspacing='0' style='width: 600px; max-width: 600px; background-color: #ffffff; border-radius: 8px;'>
<tr>
<td style='padding: 24px 32px; text-align: left;'>
<a href='{{{{Store.URL}}}}' style='font-size: 20px; font-weight: bold; color: #18181b; text-decoration: none;'>{{{{Store.Name}}}}</a>
</td>
</tr>
<tr>
<td style='padding: 0 32px;'>
<h1 style='font-size: 22px; color: #18181b; margin: 0 0 16px 0;'>{heading}</h1>
</td>
</tr>
<tr>
<td style='padding: 0 32px 24px 32px; font-size: 15px; line-height: 1.6; color: #3f3f46;'>
{content}
</td>
</tr>
<tr>
<td style='padding: 24px 32px; font-size: 12px; color: #71717a; border-top: 1px solid #e4e4e7;'>
{{% if Store.CompanyName != null and Store.CompanyName != '' %}}
{{{{Store.CompanyName}}}}<br />
{{% endif %}}
{{% if Store.CompanyAddress != null and Store.CompanyAddress != '' %}}
{{{{Store.CompanyAddress}}}}<br />
{{% endif %}}
{{% if Store.CompanyPhoneNumber != null and Store.CompanyPhoneNumber != '' %}}
{{{{Store.CompanyPhoneNumber}}}}<br />
{{% endif %}}
{{% if Store.CompanyEmail != null and Store.CompanyEmail != '' %}}
{{{{Store.CompanyEmail}}}}
{{% endif %}}
</td>
</tr>
</table>
</td>
</tr>
</table>
</body>
</html>";

    internal static string Button(string url, string text) => $@"
<table role='presentation' cellpadding='0' cellspacing='0' style='margin: 16px 0;'>
<tr>
<td style='border-radius: 6px; background-color: #18181b;'>
<a href='{url}' style='display: inline-block; padding: 12px 24px; font-size: 14px; font-weight: bold; color: #ffffff; text-decoration: none; border-radius: 6px;'>{text}</a>
</td>
</tr>
</table>";
}
