using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.CheckoutAttributes;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Services.CheckoutAttributes
{
    /// <summary>
    /// Checkout attribute helper
    /// </summary>
    public partial class CheckoutAttributeFormatter : ICheckoutAttributeFormatter
    {
        private readonly IWorkContext _workContext;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICurrencyService _currencyService;
        private readonly ITaxService _taxService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IDownloadService _downloadService;

        public CheckoutAttributeFormatter(IWorkContext workContext,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICurrencyService currencyService,
            ITaxService taxService,
            IPriceFormatter priceFormatter,
            IDownloadService downloadService)
        {
            _workContext = workContext;
            _checkoutAttributeParser = checkoutAttributeParser;
            _currencyService = currencyService;
            _taxService = taxService;
            _priceFormatter = priceFormatter;
            _downloadService = downloadService;
        }

        /// <summary>
        /// Formats attributes
        /// </summary>
        /// <param name="customAttributes">Attributes</param>
        /// <param name="customer">Customer</param>
        /// <param name="serapator">Serapator</param>
        /// <param name="htmlEncode">A value indicating whether to encode (HTML) values</param>
        /// <param name="renderPrices">A value indicating whether to render prices</param>
        /// <param name="allowHyperlinks">A value indicating whether to HTML hyperink tags could be rendered (if required)</param>
        /// <returns>Attributes</returns>
        public virtual async Task<string> FormatAttributes(IList<CustomAttribute> customAttributes,
            Customer customer,
            string serapator = "<br />",
            bool htmlEncode = true,
            bool renderPrices = true,
            bool allowHyperlinks = true)
        {
            var result = new StringBuilder();

            if (customAttributes == null || !customAttributes.Any())
                return result.ToString();

            var attributes = await _checkoutAttributeParser.ParseCheckoutAttributes(customAttributes);
            for (int i = 0; i < attributes.Count; i++)
            {
                var attribute = attributes[i];
                var valuesStr = customAttributes.Where(x => x.Key == attribute.Id).Select(x => x.Value).ToList();
                for (var j = 0; j < valuesStr.Count; j++)
                {
                    var valueStr = valuesStr[j];
                    var formattedAttribute = "";
                    if (!attribute.ShouldHaveValues())
                    {
                        //no values
                        if (attribute.AttributeControlTypeId == AttributeControlType.MultilineTextbox)
                        {
                            //multiline textbox
                            var attributeName = attribute.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id);
                            //encode (if required)
                            if (htmlEncode)
                                attributeName = WebUtility.HtmlEncode(attributeName);
                            formattedAttribute = string.Format("{0}: {1}", attributeName, FormatText.ConvertText(valueStr));
                            //we never encode multiline textbox input
                        }
                        else if (attribute.AttributeControlTypeId == AttributeControlType.FileUpload)
                        {
                            //file upload
                            Guid downloadGuid;
                            Guid.TryParse(valueStr, out downloadGuid);
                            var download = await _downloadService.GetDownloadByGuid(downloadGuid);
                            if (download != null)
                            {
                                string attributeText = "";
                                var fileName = string.Format("{0}{1}",
                                    download.Filename ?? download.DownloadGuid.ToString(),
                                    download.Extension);
                                //encode (if required)
                                if (htmlEncode)
                                    fileName = WebUtility.HtmlEncode(fileName);
                                if (allowHyperlinks)
                                {
                                    //hyperlinks are allowed
                                    var downloadLink = string.Format("{0}/download/getfileupload/?downloadId={1}", _workContext.CurrentStore.Url.TrimEnd('/'), download.DownloadGuid);
                                    attributeText = string.Format("<a href=\"{0}\" class=\"fileuploadattribute\">{1}</a>", downloadLink, fileName);
                                }
                                else
                                {
                                    //hyperlinks aren't allowed
                                    attributeText = fileName;
                                }
                                var attributeName = attribute.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id);
                                //encode (if required)
                                if (htmlEncode)
                                    attributeName = WebUtility.HtmlEncode(attributeName);
                                formattedAttribute = string.Format("{0}: {1}", attributeName, attributeText);
                            }
                        }
                        else
                        {
                            //other attributes (textbox, datepicker)
                            formattedAttribute = string.Format("{0}: {1}", attribute.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id), valueStr);
                            //encode (if required)
                            if (htmlEncode)
                                formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);
                        }
                    }
                    else
                    {
                        var attributeValue = attribute.CheckoutAttributeValues.Where(x => x.Id == valueStr).FirstOrDefault();
                        if (attributeValue != null)
                        {
                            formattedAttribute = string.Format("{0}: {1}", attribute.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id), attributeValue.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id));
                            if (renderPrices)
                            {
                                double priceAdjustmentBase = (await _taxService.GetCheckoutAttributePrice(attribute, attributeValue, customer)).checkoutPrice;
                                double priceAdjustment = await _currencyService.ConvertFromPrimaryStoreCurrency(priceAdjustmentBase, _workContext.WorkingCurrency);
                                if (priceAdjustmentBase > 0)
                                {
                                    string priceAdjustmentStr = _priceFormatter.FormatPrice(priceAdjustment);
                                    formattedAttribute += string.Format(" [+{0}]", priceAdjustmentStr);
                                }
                            }
                        }
                        //encode (if required)
                        if (htmlEncode)
                            formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);
                    }

                    if (!String.IsNullOrEmpty(formattedAttribute))
                    {
                        if (i != 0 || j != 0)
                            result.Append(serapator);
                        result.Append(formattedAttribute);
                    }
                }
            }

            return result.ToString();
        }
    }
}
