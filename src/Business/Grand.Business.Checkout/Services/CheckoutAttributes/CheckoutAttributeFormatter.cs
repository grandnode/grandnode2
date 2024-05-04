using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using System.Net;

namespace Grand.Business.Checkout.Services.CheckoutAttributes;

/// <summary>
///     Checkout attribute helper
/// </summary>
public class CheckoutAttributeFormatter : ICheckoutAttributeFormatter
{
    private readonly ICheckoutAttributeParser _checkoutAttributeParser;
    private readonly ICurrencyService _currencyService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly ITaxService _taxService;
    private readonly IWorkContext _workContext;

    public CheckoutAttributeFormatter(IWorkContext workContext,
        ICheckoutAttributeParser checkoutAttributeParser,
        ICurrencyService currencyService,
        ITaxService taxService,
        IPriceFormatter priceFormatter)
    {
        _workContext = workContext;
        _checkoutAttributeParser = checkoutAttributeParser;
        _currencyService = currencyService;
        _taxService = taxService;
        _priceFormatter = priceFormatter;
    }

    /// <summary>
    ///     Formats attributes
    /// </summary>
    /// <param name="customAttributes">Attributes</param>
    /// <param name="customer">Customer</param>
    /// <param name="separator">Separator</param>
    /// <param name="htmlEncode">A value indicating whether to encode (HTML) values</param>
    /// <param name="renderPrices">A value indicating whether to render prices</param>
    /// <param name="allowHyperlinks">A value indicating whether to HTML hyperlink tags could be rendered (if required)</param>
    /// <returns>Attributes</returns>
    public virtual async Task<string> FormatAttributes(IList<CustomAttribute> customAttributes,
        Customer customer,
        string separator = "<br />",
        bool htmlEncode = true,
        bool renderPrices = true,
        bool allowHyperlinks = true)
    {
        var result = new StringBuilder();

        if (customAttributes == null || !customAttributes.Any())
            return result.ToString();

        var attributes = await _checkoutAttributeParser.ParseCheckoutAttributes(customAttributes);
        for (var i = 0; i < attributes.Count; i++)
        {
            var attribute = attributes[i];
            var valuesStr = customAttributes.Where(x => x.Key == attribute.Id).Select(x => x.Value).ToList();
            for (var j = 0; j < valuesStr.Count; j++)
            {
                var valueStr = valuesStr[j];
                var formattedAttribute = "";
                if (!attribute.ShouldHaveValues())
                {
                    switch (attribute.AttributeControlTypeId)
                    {
                        //no values
                        case AttributeControlType.MultilineTextbox:
                        {
                            //multiline text box
                            var attributeName = attribute.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id);
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
                                var attributeName =
                                    attribute.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id);
                                if (allowHyperlinks)
                                {
                                    //hyperlinks are allowed
                                    var downloadLink =
                                        $"{_workContext.CurrentHost.Url.TrimEnd('/')}/download/getfileupload/?downloadId={downloadGuid}";
                                    attributeText =
                                        $"<a href=\"{downloadLink}\" class=\"fileuploadattribute\">{attribute.GetTranslation(a => a.TextPrompt, _workContext.WorkingLanguage.Id)}</a>";
                                }

                                formattedAttribute = $"{attributeName}: {attributeText}";
                            }

                            break;
                        }
                        default:
                        {
                            //other attributes (text box, datepicker)
                            formattedAttribute =
                                $"{attribute.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id)}: {valueStr}";
                            //encode (if required)
                            if (htmlEncode)
                                formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);
                            break;
                        }
                    }
                }
                else
                {
                    var attributeValue = attribute.CheckoutAttributeValues.FirstOrDefault(x => x.Id == valueStr);
                    if (attributeValue != null)
                    {
                        formattedAttribute =
                            $"{attribute.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id)}: {attributeValue.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id)}";
                        if (renderPrices)
                        {
                            var priceAdjustmentBase =
                                (await _taxService.GetCheckoutAttributePrice(attribute, attributeValue, customer))
                                .checkoutPrice;
                            var priceAdjustment =
                                await _currencyService.ConvertFromPrimaryStoreCurrency(priceAdjustmentBase,
                                    _workContext.WorkingCurrency);
                            if (priceAdjustmentBase > 0)
                            {
                                var priceAdjustmentStr = _priceFormatter.FormatPrice(priceAdjustment);
                                formattedAttribute += $" [+{priceAdjustmentStr}]";
                            }
                        }
                    }

                    //encode (if required)
                    if (htmlEncode)
                        formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);
                }

                if (string.IsNullOrEmpty(formattedAttribute)) continue;
                if (i != 0 || j != 0)
                    result.Append(separator);
                result.Append(formattedAttribute);
            }
        }

        return result.ToString();
    }
}