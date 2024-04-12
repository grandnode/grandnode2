using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using System.Net;

namespace Grand.Business.Catalog.Services.Products;

/// <summary>
///     Product attribute formatter
/// </summary>
public class ProductAttributeFormatter : IProductAttributeFormatter
{
    private readonly IPriceFormatter _priceFormatter;
    private readonly IPricingService _pricingService;
    private readonly IProductAttributeService _productAttributeService;
    private readonly IProductService _productService;
    private readonly ITaxService _taxService;
    private readonly IWorkContext _workContext;

    public ProductAttributeFormatter(IWorkContext workContext,
        IProductAttributeService productAttributeService,
        ITaxService taxService,
        IPriceFormatter priceFormatter,
        IPricingService priceCalculationService,
        IProductService productService)
    {
        _workContext = workContext;
        _productAttributeService = productAttributeService;
        _taxService = taxService;
        _priceFormatter = priceFormatter;
        _pricingService = priceCalculationService;
        _productService = productService;
    }

    /// <summary>
    ///     Formats attributes
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="customAttributes">Attributes</param>
    /// <returns>Attributes</returns>
    public virtual Task<string> FormatAttributes(Product product, IList<CustomAttribute> customAttributes)
    {
        var customer = _workContext.CurrentCustomer;
        return FormatAttributes(product, customAttributes, customer);
    }

    /// <summary>
    ///     Formats attributes
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="customAttributes">Attributes</param>
    /// <param name="customer">Customer</param>
    /// <param name="separator">Separator</param>
    /// <param name="htmlEncode">A value indicating whether to encode (HTML) values</param>
    /// <param name="renderPrices">A value indicating whether to render prices</param>
    /// <param name="renderProductAttributes">A value indicating whether to render product attributes</param>
    /// <param name="renderGiftVoucherAttributes">A value indicating whether to render gift voucher attributes</param>
    /// <param name="allowHyperlinks">A value indicating whether to HTML hyperlink tags could be rendered (if required)</param>
    /// <param name="showInAdmin">Show in admin</param>
    /// <returns>Attributes</returns>
    public virtual async Task<string> FormatAttributes(Product product, IList<CustomAttribute> customAttributes,
        Customer customer, string separator = "<br />", bool htmlEncode = true, bool renderPrices = true,
        bool renderProductAttributes = true, bool renderGiftVoucherAttributes = true,
        bool allowHyperlinks = true, bool showInAdmin = false)
    {
        var result = new StringBuilder();

        if (customAttributes == null || !customAttributes.Any())
            return result.ToString();

        var langId = _workContext.WorkingLanguage != null
            ? _workContext.WorkingLanguage.Id
            : customer?.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId);

        if (string.IsNullOrEmpty(langId))
            langId = "";

        //attributes
        if (renderProductAttributes)
        {
            result = await PrepareFormattedAttribute(product, customAttributes, langId, separator, htmlEncode,
                renderPrices, allowHyperlinks, showInAdmin);

            if (product.ProductTypeId == ProductType.BundledProduct)
            {
                var i = 0;
                if (result.Length > 0) result.Append(separator);
                foreach (var bundle in product.BundleProducts)
                {
                    var p1 = await _productService.GetProductById(bundle.ProductId);
                    if (p1 == null) continue;
                    if (i > 0)
                        result.Append(separator);

                    if (p1.VisibleIndividually)
                        result.Append(
                            $"<a href=\"{p1.GetSeName(langId)}\"> {(htmlEncode ? WebUtility.HtmlEncode(p1.GetTranslation(x => x.Name, langId)) : p1.GetTranslation(x => x.Name, langId))} </a>");
                    else
                        result.Append(
                            $"{(htmlEncode ? WebUtility.HtmlEncode(p1.GetTranslation(x => x.Name, langId)) : p1.GetTranslation(x => x.Name, langId))}");
                    var formattedAttribute = await PrepareFormattedAttribute(p1, customAttributes, langId, separator,
                        htmlEncode,
                        renderPrices, allowHyperlinks, showInAdmin);
                    if (formattedAttribute.Length > 0)
                    {
                        result.Append(separator);
                        result.Append(formattedAttribute);
                    }

                    i++;
                }
            }
        }

        //gift vouchers
        if (!renderGiftVoucherAttributes) return result.ToString();
        if (!product.IsGiftVoucher) return result.ToString();
        GiftVoucherExtensions.GetGiftVoucherAttribute(customAttributes, out var giftVoucherRecipientName,
            out var giftVoucherRecipientEmail,
            out var giftVoucherSenderName, out var giftVoucherSenderEmail, out _);

        //sender
        var giftVoucherFrom = product.GiftVoucherTypeId == GiftVoucherType.Virtual
            ? $"{giftVoucherSenderName} <{giftVoucherSenderEmail}>"
            : $"{giftVoucherSenderName}";
        //recipient
        var giftVoucherFor = product.GiftVoucherTypeId == GiftVoucherType.Virtual
            ? $"{giftVoucherRecipientName} <{giftVoucherRecipientEmail}>"
            : $"{giftVoucherRecipientName}";

        //encode (if required)
        if (htmlEncode)
        {
            giftVoucherFrom = WebUtility.HtmlEncode(giftVoucherFrom);
            giftVoucherFor = WebUtility.HtmlEncode(giftVoucherFor);
        }

        if (!string.IsNullOrEmpty(result.ToString())) result.Append(separator);
        result.Append(giftVoucherFrom);
        result.Append(separator);
        result.Append(giftVoucherFor);
        return result.ToString();
    }

    private async Task<StringBuilder> PrepareFormattedAttribute(Product product,
        IList<CustomAttribute> customAttributes, string langId,
        string separator, bool htmlEncode, bool renderPrices,
        bool allowHyperlinks, bool showInAdmin)
    {
        var result = new StringBuilder();
        var attributes = product.ParseProductAttributeMappings(customAttributes);
        for (var i = 0; i < attributes.Count; i++)
        {
            var productAttribute =
                await _productAttributeService.GetProductAttributeById(attributes[i].ProductAttributeId);
            if (productAttribute == null)
                continue;

            var attribute = attributes[i];
            var valuesStr = ProductExtensions.ParseValues(customAttributes, attribute.Id);
            for (var j = 0; j < valuesStr.Count; j++)
            {
                var valueStr = valuesStr[j];
                var formattedAttribute = string.Empty;
                if (!attribute.ShouldHaveValues())
                {
                    switch (attribute.AttributeControlTypeId)
                    {
                        //no values
                        case AttributeControlType.MultilineTextbox:
                        {
                            //multiline text
                            var attributeName = productAttribute.GetTranslation(a => a.Name, langId);
                            //encode (if required)
                            if (htmlEncode)
                                attributeName = WebUtility.HtmlEncode(attributeName);
                            formattedAttribute = $"{attributeName}: {FormatText.ConvertText(valueStr)}";
                            //we never encode multiline text box input
                            break;
                        }
                        case AttributeControlType.FileUpload:
                        {
                            //file upload
                            if (Guid.TryParse(valueStr, out var downloadGuid))
                            {
                                var attributeText = string.Empty;
                                var attributeName = productAttribute.GetTranslation(a => a.Name, langId);
                                if (allowHyperlinks)
                                {
                                    var downloadLink =
                                        $"{_workContext.CurrentHost.Url.TrimEnd('/')}/download/getfileupload/?downloadId={downloadGuid}";
                                    attributeText =
                                        $"<a href=\"{downloadLink}\" class=\"fileuploadattribute\">{attribute.GetTranslation(a => a.TextPrompt, langId)}</a>";
                                }

                                formattedAttribute = $"{attributeName}: {attributeText}";
                            }

                            break;
                        }
                        default:
                        {
                            //other attributes (text box, datepicker)
                            formattedAttribute =
                                $"{productAttribute.GetTranslation(a => a.Name, langId)}: {valueStr}";
                            //encode (if required)
                            if (htmlEncode)
                                formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);
                            break;
                        }
                    }
                }
                else
                {
                    //attributes with values
                    if (product.ProductAttributeMappings.FirstOrDefault(x => x.Id == attributes[i].Id) != null)
                    {
                        var attributeValue = product.ProductAttributeMappings
                            .FirstOrDefault(x => x.Id == attributes[i].Id).ProductAttributeValues
                            .FirstOrDefault(x => x.Id == valueStr);
                        if (attributeValue != null)
                        {
                            formattedAttribute =
                                $"{productAttribute.GetTranslation(a => a.Name, langId)}: {attributeValue.GetTranslation(a => a.Name, langId)}";

                            if (renderPrices)
                            {
                                var attributeValuePriceAdjustment =
                                    await _pricingService.GetProductAttributeValuePriceAdjustment(attributeValue);
                                var (priceAdjustmentBase, _) = await _taxService.GetProductPrice(product,
                                    attributeValuePriceAdjustment, _workContext.CurrentCustomer);
                                switch (priceAdjustmentBase)
                                {
                                    case > 0:
                                    {
                                        var priceAdjustmentStr = _priceFormatter.FormatPrice(priceAdjustmentBase,
                                            _workContext.WorkingCurrency);
                                        formattedAttribute += $" [+{priceAdjustmentStr}]";
                                        break;
                                    }
                                    case < 0:
                                    {
                                        var priceAdjustmentStr = _priceFormatter.FormatPrice(-priceAdjustmentBase,
                                            _workContext.WorkingCurrency);
                                        formattedAttribute += $" [-{priceAdjustmentStr}]";
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (showInAdmin)
                                formattedAttribute +=
                                    $"{productAttribute.GetTranslation(a => a.Name, langId)}: ";
                        }

                        //encode (if required)
                        if (htmlEncode)
                            formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);
                    }
                }

                if (string.IsNullOrEmpty(formattedAttribute)) continue;
                if (i != 0 || j != 0)
                    result.Append(separator);
                result.Append(formattedAttribute);
            }
        }

        return result;
    }
}