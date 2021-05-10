using Grand.Business.Common.Extensions;
using Grand.Business.Marketing.Extensions;
using Grand.Business.Marketing.Interfaces.Contacts;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Services.Contacts
{
    /// <summary>
    /// Contact attribute parser
    /// </summary>
    public partial class ContactAttributeParser : IContactAttributeParser
    {
        private readonly IContactAttributeService _contactAttributeService;
        private readonly IDownloadService _downloadService;
        private readonly IWorkContext _workContext;

        public ContactAttributeParser(
            IContactAttributeService contactAttributeService,
            IDownloadService downloadService,
            IWorkContext workContext
            )
        {
            _contactAttributeService = contactAttributeService;
            _downloadService = downloadService;
            _workContext = workContext;
        }

        /// <summary>
        /// Gets selected contact attributes
        /// </summary>
        /// <param name="customAttributes">Attributes</param>
        /// <returns>Selected contact attributes</returns>
        public virtual async Task<IList<ContactAttribute>> ParseContactAttributes(IList<CustomAttribute> customAttributes)
        {
            var result = new List<ContactAttribute>();
            if (customAttributes == null || !customAttributes.Any())
                return result;

            foreach (var customAttribute in customAttributes.GroupBy(x => x.Key))
            {
                var attribute = await _contactAttributeService.GetContactAttributeById(customAttribute.Key);
                if (attribute != null)
                {
                    result.Add(attribute);
                }
            }
            return result;
        }

        /// <summary>
        /// Get contact attribute values
        /// </summary>
        /// <param name="attributes">Attributes</param>
        /// <returns>Contact attribute values</returns>
        public virtual async Task<IList<ContactAttributeValue>> ParseContactAttributeValues(IList<CustomAttribute> customAttributes)
        {
            var values = new List<ContactAttributeValue>();
            if (customAttributes == null || !customAttributes.Any())
                return values;

            var attributes = await ParseContactAttributes(customAttributes);
            foreach (var attribute in attributes)
            {
                if (!attribute.ShouldHaveValues())
                    continue;

                var valuesStr = customAttributes.Where(x => x.Key == attribute.Id).Select(x => x.Value);
                foreach (var valueStr in valuesStr)
                {
                    if (!string.IsNullOrEmpty(valueStr))
                    {
                        var value = attribute.ContactAttributeValues.Where(x => x.Id == valueStr).FirstOrDefault();
                        if (value != null)
                            values.Add(value);
                    }
                }
            }
            return values;
        }
        /// <summary>
        /// Adds an attribute
        /// </summary>
        /// <param name="attributes">Attributes</param>
        /// <param name="ca">Contact attribute</param>
        /// <param name="value">Value</param>
        /// <returns>Attributes</returns>
        public virtual IList<CustomAttribute> AddContactAttribute(IList<CustomAttribute> customAttributes, ContactAttribute ca, string value)
        {
            if (customAttributes == null)
                customAttributes = new List<CustomAttribute>();

            customAttributes.Add(new CustomAttribute() { Key = ca.Id, Value = value });

            return customAttributes;

        }

        /// <summary>
        /// Check whether condition of some attribute is met (if specified). Return "null" if not condition is specified
        /// </summary>
        /// <param name="attribute">Contact attribute</param>
        /// <param name="selectedAttributes">Selected attributes</param>
        /// <returns>Result</returns>
        public virtual async Task<bool?> IsConditionMet(ContactAttribute attribute, IList<CustomAttribute> customAttributes)
        {
            if (attribute == null)
                throw new ArgumentNullException(nameof(attribute));

            if (customAttributes == null)
                customAttributes = new List<CustomAttribute>();

            var conditionAttribute = attribute.ConditionAttribute;
            if (!conditionAttribute.Any())
                //no condition
                return null;

            //load an attribute this one depends on
            var dependOnAttribute = (await ParseContactAttributes(conditionAttribute)).FirstOrDefault();
            if (dependOnAttribute == null)
                return true;

            var valuesThatShouldBeSelected = conditionAttribute.Where(x => x.Key == dependOnAttribute.Id).Select(x => x.Value)
                //a workaround here:
                //ConditionAttribute can contain "empty" values (nothing is selected)
                //but in other cases (like below) we do not store empty values
                //that's why we remove empty values here
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            var selectedValues = customAttributes.Where(x => x.Key == dependOnAttribute.Id).Select(x => x.Value).ToList();
            if (valuesThatShouldBeSelected.Count != selectedValues.Count)
                return false;

            //compare values
            var allFound = true;
            foreach (var t1 in valuesThatShouldBeSelected)
            {
                bool found = false;
                foreach (var t2 in selectedValues)
                    if (t1 == t2)
                        found = true;
                if (!found)
                    allFound = false;
            }

            return allFound;
        }

        /// <summary>
        /// Remove an attribute
        /// </summary>
        /// <param name="attributes">Attributes</param>
        /// <param name="attribute">Contact attribute</param>
        /// <returns>Updated result</returns>
        public virtual IList<CustomAttribute> RemoveContactAttribute(IList<CustomAttribute> customAttributes, ContactAttribute attribute)
        {
            return customAttributes.Where(x => x.Key != attribute.Id).ToList();
        }

        /// <summary>
        /// Formats attributes
        /// </summary>
        /// <param name="language">Language</param>
        /// <param name="customAttributes">Attributes </param>
        /// <param name="customer">Customer</param>
        /// <param name="serapator">Serapator</param>
        /// <param name="htmlEncode">A value indicating whether to encode (HTML) values</param>
        /// <param name="allowHyperlinks">A value indicating whether to HTML hyperink tags could be rendered (if required)</param>
        /// <returns>Attributes</returns>
        public virtual async Task<string> FormatAttributes(
            Language language,
            IList<CustomAttribute> customAttributes,
            Customer customer,
            string serapator = "<br />",
            bool htmlEncode = true,
            bool allowHyperlinks = true)
        {
            var result = new StringBuilder();
            if (customAttributes == null || !customAttributes.Any())
                return result.ToString();

            var attributes = await ParseContactAttributes(customAttributes);
            for (var i = 0; i < attributes.Count; i++)
            {
                var attribute = attributes[i];
                var valuesStr = customAttributes.Where(x => x.Key == attribute.Id).Select(x => x.Value).ToList();
                for (int j = 0; j < valuesStr.Count; j++)
                {
                    string valueStr = valuesStr[j];
                    string formattedAttribute = "";
                    if (!attribute.ShouldHaveValues())
                    {
                        //no values
                        if (attribute.AttributeControlType == AttributeControlType.MultilineTextbox)
                        {
                            //multiline textbox
                            var attributeName = attribute.GetTranslation(a => a.Name, language.Id);
                            //encode (if required)
                            if (htmlEncode)
                                attributeName = WebUtility.HtmlEncode(attributeName);
                            formattedAttribute = string.Format("{0}: {1}", attributeName, FormatText.ConvertText(valueStr));
                            //we never encode multiline textbox input
                        }
                        else if (attribute.AttributeControlType == AttributeControlType.FileUpload)
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
                                    var downloadLink = string.Format("{0}download/getfileupload/?downloadId={1}", _workContext.CurrentStore.Url.TrimEnd('/'), download.DownloadGuid);
                                    attributeText = string.Format("<a href=\"{0}\" class=\"fileuploadattribute\">{1}</a>", downloadLink, fileName);
                                }
                                else
                                {
                                    //hyperlinks aren't allowed
                                    attributeText = fileName;
                                }
                                var attributeName = attribute.GetTranslation(a => a.Name, language.Id);
                                //encode (if required)
                                if (htmlEncode)
                                    attributeName = WebUtility.HtmlEncode(attributeName);
                                formattedAttribute = string.Format("{0}: {1}", attributeName, attributeText);
                            }
                        }
                        else
                        {
                            //other attributes (textbox, datepicker)
                            formattedAttribute = string.Format("{0}: {1}", attribute.GetTranslation(a => a.Name, language.Id), valueStr);
                            //encode (if required)
                            if (htmlEncode)
                                formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);
                        }
                    }
                    else
                    {
                        var attributeValue = attribute.ContactAttributeValues.Where(x => x.Id == valueStr).FirstOrDefault();
                        if (attributeValue != null)
                        {
                            formattedAttribute = string.Format("{0}: {1}", attribute.GetTranslation(a => a.Name, language.Id), attributeValue.GetTranslation(a => a.Name, language.Id));
                        }
                        //encode (if required)
                        if (htmlEncode)
                            formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);
                    }

                    if (!string.IsNullOrEmpty(formattedAttribute))
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
