using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using System.Net;

namespace Grand.Business.Catalog.Services.Products
{
    /// <summary>
    /// Product attribute formatter
    /// </summary>
    public partial class ProductAttributeFormatter : IProductAttributeFormatter
    {
        private readonly IWorkContext _workContext;
        private readonly IProductAttributeService _productAttributeService;
        private readonly ITranslationService _translationService;
        private readonly ITaxService _taxService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IPricingService _pricingService;
        private readonly IProductService _productService;

        public ProductAttributeFormatter(IWorkContext workContext,
            IProductAttributeService productAttributeService,
            ITranslationService translationService,
            ITaxService taxService,
            IPriceFormatter priceFormatter,
            IPricingService priceCalculationService,
            IProductService productService)
        {
            _workContext = workContext;
            _productAttributeService = productAttributeService;
            _translationService = translationService;
            _taxService = taxService;
            _priceFormatter = priceFormatter;
            _pricingService = priceCalculationService;
            _productService = productService;
        }

        /// <summary>
        /// Formats attributes
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
        /// Formats attributes
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customAttributes">Attributes</param>
        /// <param name="customer">Customer</param>
        /// <param name="serapator">Serapator</param>
        /// <param name="htmlEncode">A value indicating whether to encode (HTML) values</param>
        /// <param name="renderPrices">A value indicating whether to render prices</param>
        /// <param name="renderProductAttributes">A value indicating whether to render product attributes</param>
        /// <param name="renderGiftVoucherAttributes">A value indicating whether to render gift voucher attributes</param>
        /// <param name="allowHyperlinks">A value indicating whether to HTML hyperink tags could be rendered (if required)</param>
        /// <returns>Attributes</returns>
        public virtual async Task<string> FormatAttributes(Product product, IList<CustomAttribute> customAttributes,
            Customer customer, string serapator = "<br />", bool htmlEncode = true, bool renderPrices = true,
            bool renderProductAttributes = true, bool renderGiftVoucherAttributes = true,
            bool allowHyperlinks = true, bool showInAdmin = false)
        {

            var result = new StringBuilder();

            if (customAttributes == null || !customAttributes.Any())
                return result.ToString();

            var langId = string.Empty;

            if (_workContext.WorkingLanguage != null)
                langId = _workContext.WorkingLanguage.Id;
            else
                langId = customer?.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId);

            if (string.IsNullOrEmpty(langId))
                langId = "";

            //attributes
            if (renderProductAttributes)
            {
                result = await PrepareFormattedAttribute(product, customAttributes, langId, serapator, htmlEncode,
                    renderPrices, allowHyperlinks, showInAdmin);

                if (product.ProductTypeId == ProductType.BundledProduct)
                {
                    int i = 0;
                    foreach (var bundle in product.BundleProducts)
                    {
                        var p1 = await _productService.GetProductById(bundle.ProductId);
                        if (p1 != null)
                        {
                            if (i > 0)
                                result.Append(serapator);

                            result.Append($"<a href=\"{p1.GetSeName(langId)}\"> {p1.GetTranslation(x => x.Name, langId)} </a>");
                            var formattedAttribute = await PrepareFormattedAttribute(p1, customAttributes, langId, serapator, htmlEncode,
                            renderPrices, allowHyperlinks, showInAdmin);
                            if (formattedAttribute.Length > 0)
                            {
                                result.Append(serapator);
                                result.Append(formattedAttribute);
                            }
                            i++;
                        }
                    }
                }

            }

            //gift vouchers
            if (renderGiftVoucherAttributes)
            {
                if (product.IsGiftVoucher)
                {
                    GiftVoucherExtensions.GetGiftVoucherAttribute(customAttributes, out var giftVoucherRecipientName, out var giftVoucherRecipientEmail,
                        out var giftVoucherSenderName, out var giftVoucherSenderEmail, out var giftVoucherMessage);

                    //sender
                    var giftVoucherFrom = product.GiftVoucherTypeId == GiftVoucherType.Virtual ?
                        string.Format(_translationService.GetResource("GiftVoucherAttribute.From.Virtual"), giftVoucherSenderName, giftVoucherSenderEmail) :
                        string.Format(_translationService.GetResource("GiftVoucherAttribute.From.Physical"), giftVoucherSenderName);
                    //recipient
                    var giftVoucherFor = product.GiftVoucherTypeId == GiftVoucherType.Virtual ?
                        string.Format(_translationService.GetResource("GiftVoucherAttribute.For.Virtual"), giftVoucherRecipientName, giftVoucherRecipientEmail) :
                        string.Format(_translationService.GetResource("GiftVoucherAttribute.For.Physical"), giftVoucherRecipientName);

                    //encode (if required)
                    if (htmlEncode)
                    {
                        giftVoucherFrom = WebUtility.HtmlEncode(giftVoucherFrom);
                        giftVoucherFor = WebUtility.HtmlEncode(giftVoucherFor);
                    }

                    if (!String.IsNullOrEmpty(result.ToString()))
                    {
                        result.Append(serapator);
                    }
                    result.Append(giftVoucherFrom);
                    result.Append(serapator);
                    result.Append(giftVoucherFor);
                }
            }
            return result.ToString();
        }

        private async Task<StringBuilder> PrepareFormattedAttribute(Product product, IList<CustomAttribute> customAttributes, string langId,
            string serapator, bool htmlEncode, bool renderPrices,
            bool allowHyperlinks, bool showInAdmin)
        {
            var result = new StringBuilder();
            var attributes = product.ParseProductAttributeMappings(customAttributes);
            for (int i = 0; i < attributes.Count; i++)
            {
                var productAttribute = await _productAttributeService.GetProductAttributeById(attributes[i].ProductAttributeId);
                if (productAttribute == null)
                    continue;

                var attribute = attributes[i];
                var valuesStr = ProductExtensions.ParseValues(customAttributes, attribute.Id);
                for (int j = 0; j < valuesStr.Count; j++)
                {
                    string valueStr = valuesStr[j];
                    string formattedAttribute = string.Empty;
                    if (!attribute.ShouldHaveValues())
                    {
                        //no values
                        if (attribute.AttributeControlTypeId == AttributeControlType.MultilineTextbox)
                        {
                            //multiline textbox
                            var attributeName = productAttribute.GetTranslation(a => a.Name, langId);
                            //encode (if required)
                            if (htmlEncode)
                                attributeName = WebUtility.HtmlEncode(attributeName);
                            formattedAttribute = string.Format("{0}: {1}", attributeName, FormatText.ConvertText(valueStr));
                            //we never encode multiline textbox input
                        }
                        else if (attribute.AttributeControlTypeId == AttributeControlType.FileUpload)
                        {
                            //file upload
                            if (Guid.TryParse(valueStr, out var downloadGuid))
                            {
                                var attributeText = string.Empty;
                                var attributeName = productAttribute.GetTranslation(a => a.Name, langId);
                                if (allowHyperlinks)
                                {
                                    var downloadLink = string.Format("{0}/download/getfileupload/?downloadId={1}", _workContext.CurrentHost.Url.TrimEnd('/'), downloadGuid);
                                    attributeText = string.Format("<a href=\"{0}\" class=\"fileuploadattribute\">{1}</a>", downloadLink, attribute.GetTranslation(a => a.TextPrompt, langId));
                                }
                                formattedAttribute = string.Format("{0}: {1}", attributeName, attributeText);
                            }
                        }
                        else
                        {
                            //other attributes (textbox, datepicker)
                            formattedAttribute = string.Format("{0}: {1}", productAttribute.GetTranslation(a => a.Name, langId), valueStr);
                            //encode (if required)
                            if (htmlEncode)
                                formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);
                        }
                    }
                    else
                    {
                        //attributes with values
                        if (product.ProductAttributeMappings.Where(x => x.Id == attributes[i].Id).FirstOrDefault() != null)
                        {

                            var attributeValue = product.ProductAttributeMappings.Where(x => x.Id == attributes[i].Id).FirstOrDefault().ProductAttributeValues.Where(x => x.Id == valueStr).FirstOrDefault();
                            if (attributeValue != null)
                            {
                                formattedAttribute = string.Format("{0}: {1}", productAttribute.GetTranslation(a => a.Name, langId), attributeValue.GetTranslation(a => a.Name, langId));

                                if (renderPrices)
                                {
                                    double attributeValuePriceAdjustment = await _pricingService.GetProductAttributeValuePriceAdjustment(attributeValue);
                                    var prices = await _taxService.GetProductPrice(product, attributeValuePriceAdjustment, _workContext.CurrentCustomer);
                                    double priceAdjustmentBase = prices.productprice;
                                    double taxRate = prices.taxRate;
                                    if (priceAdjustmentBase > 0)
                                    {
                                        string priceAdjustmentStr = _priceFormatter.FormatPrice(priceAdjustmentBase, false);
                                        formattedAttribute += string.Format(" [+{0}]", priceAdjustmentStr);
                                    }
                                    else if (priceAdjustmentBase < 0)
                                    {
                                        string priceAdjustmentStr = _priceFormatter.FormatPrice(-priceAdjustmentBase, false);
                                        formattedAttribute += string.Format(" [-{0}]", priceAdjustmentStr);
                                    }
                                }

                            }
                            else
                            {
                                if (showInAdmin)
                                    formattedAttribute += string.Format("{0}: {1}", productAttribute.GetTranslation(a => a.Name, langId), "");
                            }

                            //encode (if required)
                            if (htmlEncode)
                                formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);
                        }
                    }

                    if (!string.IsNullOrEmpty(formattedAttribute))
                    {
                        if (i != 0 || j != 0)
                            result.Append(serapator);
                        result.Append(formattedAttribute);
                    }
                }
            }
            return result;
        }
    }
}
