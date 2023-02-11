using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.SharedKernel.Extensions;
using System.Net;

namespace Grand.Business.Customers.Services
{
    /// <summary>
    /// Customer attribute parser
    /// </summary>
    public class CustomerAttributeParser : ICustomerAttributeParser
    {
        private readonly ICustomerAttributeService _customerAttributeService;
        private readonly ITranslationService _translationService;

        public CustomerAttributeParser(ICustomerAttributeService customerAttributeService,
            ITranslationService translationService)
        {
            _customerAttributeService = customerAttributeService;
            _translationService = translationService;
        }

        /// <summary>
        /// Gets selected customer attributes
        /// </summary>
        /// <param name="customAttributes">Attributes</param>
        /// <returns>Selected customer attributes</returns>
        public virtual async Task<IList<CustomerAttribute>> ParseCustomerAttributes(IList<CustomAttribute> customAttributes)
        {
            var result = new List<CustomerAttribute>();
            if (customAttributes == null || !customAttributes.Any())
                return result;

            foreach (var customAttribute in customAttributes.GroupBy(x => x.Key))
            {
                var attribute = await _customerAttributeService.GetCustomerAttributeById(customAttribute.Key);
                if (attribute != null)
                {
                    result.Add(attribute);
                }
            }
            return result;
        }

        /// <summary>
        /// Get customer attribute values
        /// </summary>
        /// <param name="customAttributes">Attributes</param>
        /// <returns>Customer attribute values</returns>
        public virtual async Task<IList<CustomerAttributeValue>> ParseCustomerAttributeValues(IList<CustomAttribute> customAttributes)
        {
            var values = new List<CustomerAttributeValue>();
            if (customAttributes == null || !customAttributes.Any())
                return values;

            var attributes = await ParseCustomerAttributes(customAttributes);
            foreach (var attribute in attributes)
            {
                if (!attribute.ShouldHaveValues())
                    continue;

                var valuesStr = customAttributes.Where(x => x.Key == attribute.Id).Select(x => x.Value);
                values.AddRange(from valueStr in valuesStr where !string.IsNullOrEmpty(valueStr) select attribute.CustomerAttributeValues.FirstOrDefault(x => x.Id == valueStr) into value where value != null select value);
            }
            return values;
        }


        /// <summary>
        /// Adds an attribute
        /// </summary>
        /// <param name="customAttributes">Attributes</param>
        /// <param name="ca">Customer attribute</param>
        /// <param name="value">Value</param>
        /// <returns>Attributes</returns>
        public virtual IList<CustomAttribute> AddCustomerAttribute(IList<CustomAttribute> customAttributes, CustomerAttribute ca, string value)
        {
            customAttributes ??= new List<CustomAttribute>();
            customAttributes.Add(new CustomAttribute { Key = ca.Id, Value = value });
            return customAttributes;
        }

        /// <summary>
        /// Validates customer attributes
        /// </summary>
        /// <param name="customAttributes">Attributes</param>
        /// <returns>Warnings</returns>
        public virtual async Task<IList<string>> GetAttributeWarnings(IList<CustomAttribute> customAttributes)
        {
            var warnings = new List<string>();

            if (customAttributes == null || !customAttributes.Any())
                return warnings;

            //ensure it's our attributes
            var attributes1 = await ParseCustomerAttributes(customAttributes);

            //validate required customer attributes (whether they're chosen/selected/entered)
            var attributes2 = await _customerAttributeService.GetAllCustomerAttributes();
            foreach (var a2 in attributes2)
            {
                if (!a2.IsRequired) continue;
                var found = false;
                //selected customer attributes
                foreach (var a1 in attributes1)
                {
                    if (a1.Id != a2.Id) continue;
                    var valuesStr = customAttributes.Where(x => x.Key == a1.Id).Select(x => x.Value);
                    if (valuesStr.Any(str1 => !string.IsNullOrEmpty(str1.Trim())))
                    {
                        found = true;
                    }
                }

                //if not found
                if (found) continue;
                var notFoundWarning = string.Format(_translationService.GetResource("ShoppingCart.SelectAttribute"), a2.GetTranslation(a => a.Name, ""));

                warnings.Add(notFoundWarning);
            }

            return warnings;
        }

        /// <summary>
        /// Formats attributes
        /// </summary>
        /// <param name="language">Language</param>
        /// <param name="customAttributes">Attributes</param>
        /// <param name="separator">Separator</param>
        /// <param name="htmlEncode">A value indicating whether to encode (HTML) values</param>
        /// <returns>Attributes</returns>
        public virtual async Task<string> FormatAttributes(Language language,
            IList<CustomAttribute> customAttributes, string separator = "<br />", bool htmlEncode = true)
        {
            var result = new StringBuilder();

            var attributes = await ParseCustomerAttributes(customAttributes);
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
                        //no values
                        if (attribute.AttributeControlTypeId == AttributeControlType.MultilineTextbox)
                        {
                            //multiline text box
                            var attributeName = attribute.GetTranslation(a => a.Name, language.Id);
                            //encode (if required)
                            if (htmlEncode)
                                attributeName = WebUtility.HtmlEncode(attributeName);
                            formattedAttribute = $"{attributeName}: {FormatText.ConvertText(valueStr)}";
                            //we never encode multiline text box input
                        }
                        else if (attribute.AttributeControlTypeId == AttributeControlType.FileUpload)
                        {
                            //file upload
                            //not supported for customer attributes
                        }
                        else
                        {
                            //other attributes (text box, datepicker)
                            formattedAttribute = $"{attribute.GetTranslation(a => a.Name, language.Id)}: {valueStr}";
                            //encode (if required)
                            if (htmlEncode)
                                formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);
                        }
                    }
                    else
                    {

                        var attributeValueId = valueStr;
                        var attributeValue = attribute.CustomerAttributeValues.FirstOrDefault(x => x.Id == attributeValueId);
                        if (attributeValue != null)
                        {
                            formattedAttribute =
                                $"{attribute.GetTranslation(a => a.Name, language.Id)}: {attributeValue.GetTranslation(a => a.Name, language.Id)}";
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
}
