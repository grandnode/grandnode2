using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using System.Net;

namespace Grand.Business.Marketing.Services.Contacts;

/// <summary>
///     Contact attribute parser
/// </summary>
public class ContactAttributeParser : IContactAttributeParser
{
    private readonly IContactAttributeService _contactAttributeService;
    private readonly IWorkContext _workContext;

    public ContactAttributeParser(
        IContactAttributeService contactAttributeService,
        IWorkContext workContext
    )
    {
        _contactAttributeService = contactAttributeService;
        _workContext = workContext;
    }

    /// <summary>
    ///     Gets selected contact attributes
    /// </summary>
    /// <param name="customAttributes">Attributes</param>
    /// <returns>Selected contact attributes</returns>
    public virtual async Task<IList<ContactAttribute>> ParseContactAttributes(
        IList<CustomAttribute> customAttributes)
    {
        var result = new List<ContactAttribute>();
        if (customAttributes == null || !customAttributes.Any())
            return result;

        foreach (var customAttribute in customAttributes.GroupBy(x => x.Key))
        {
            var attribute = await _contactAttributeService.GetContactAttributeById(customAttribute.Key);
            if (attribute != null) result.Add(attribute);
        }

        return result;
    }

    /// <summary>
    ///     Get contact attribute values
    /// </summary>
    /// <param name="customAttributes">Attributes</param>
    /// <returns>Contact attribute values</returns>
    public virtual async Task<IList<ContactAttributeValue>> ParseContactAttributeValues(
        IList<CustomAttribute> customAttributes)
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
            values.AddRange(from valueStr in valuesStr
                where !string.IsNullOrEmpty(valueStr)
                select attribute.ContactAttributeValues.FirstOrDefault(x => x.Id == valueStr)
                into value
                where value != null
                select value);
        }

        return values;
    }

    /// <summary>
    ///     Adds an attribute
    /// </summary>
    /// <param name="customAttributes">Attributes</param>
    /// <param name="ca">Contact attribute</param>
    /// <param name="value">Value</param>
    /// <returns>Attributes</returns>
    public virtual IList<CustomAttribute> AddContactAttribute(IList<CustomAttribute> customAttributes,
        ContactAttribute ca, string value)
    {
        customAttributes ??= new List<CustomAttribute>();
        customAttributes.Add(new CustomAttribute { Key = ca.Id, Value = value });
        return customAttributes;
    }

    /// <summary>
    ///     Check whether condition of some attribute is met (if specified). Return "null" if not condition is specified
    /// </summary>
    /// <param name="attribute">Contact attribute</param>
    /// <param name="customAttributes">Selected attributes</param>
    /// <returns>Result</returns>
    public virtual async Task<bool?> IsConditionMet(ContactAttribute attribute,
        IList<CustomAttribute> customAttributes)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        customAttributes ??= new List<CustomAttribute>();

        var conditionAttribute = attribute.ConditionAttribute;
        if (!conditionAttribute.Any())
            //no condition
            return null;

        //load an attribute this one depends on
        var dependOnAttribute = (await ParseContactAttributes(conditionAttribute)).FirstOrDefault();
        if (dependOnAttribute == null)
            return true;

        var valuesThatShouldBeSelected = conditionAttribute.Where(x => x.Key == dependOnAttribute.Id)
            .Select(x => x.Value)
            //a workaround here:
            //ConditionAttribute can contain "empty" values (nothing is selected)
            //but in other cases (like below) we do not store empty values
            //that's why we remove empty values here
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        var selectedValues = customAttributes.Where(x => x.Key == dependOnAttribute.Id).Select(x => x.Value)
            .ToList();
        if (valuesThatShouldBeSelected.Count != selectedValues.Count)
            return false;

        //compare values
        var allFound = true;
        foreach (var t1 in valuesThatShouldBeSelected)
        {
            var found = false;
            foreach (var t2 in selectedValues.Where(t2 => t1 == t2))
                found = true;
            if (!found)
                allFound = false;
        }

        return allFound;
    }

    /// <summary>
    ///     Remove an attribute
    /// </summary>
    /// <param name="customAttributes">Attributes</param>
    /// <param name="attribute">Contact attribute</param>
    /// <returns>Updated result</returns>
    public virtual IList<CustomAttribute> RemoveContactAttribute(IList<CustomAttribute> customAttributes,
        ContactAttribute attribute)
    {
        return customAttributes.Where(x => x.Key != attribute.Id).ToList();
    }

    /// <summary>
    ///     Formats attributes
    /// </summary>
    /// <param name="language">Language</param>
    /// <param name="customAttributes">Attributes </param>
    /// <param name="customer">Customer</param>
    /// <param name="separator">Separator</param>
    /// <param name="htmlEncode">A value indicating whether to encode (HTML) values</param>
    /// <param name="allowHyperlinks">A value indicating whether to HTML hyperlink tags could be rendered (if required)</param>
    /// <returns>Attributes</returns>
    public virtual async Task<string> FormatAttributes(
        Language language,
        IList<CustomAttribute> customAttributes,
        Customer customer,
        string separator = "<br />",
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
            for (var j = 0; j < valuesStr.Count; j++)
            {
                var valueStr = valuesStr[j];
                var formattedAttribute = "";
                if (!attribute.ShouldHaveValues())
                {
                    switch (attribute.AttributeControlType)
                    {
                        //no values
                        case AttributeControlType.MultilineTextbox:
                        {
                            //multiline text box
                            var attributeName = attribute.GetTranslation(a => a.Name, language.Id);
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
                                var attributeName = attribute.GetTranslation(a => a.Name, language.Id);
                                if (allowHyperlinks)
                                {
                                    var downloadLink =
                                        $"{_workContext.CurrentHost.Url.TrimEnd('/')}/download/getfileupload/?downloadId={downloadGuid}";
                                    attributeText =
                                        $"<a href=\"{downloadLink}\" class=\"fileuploadattribute\">{attribute.GetTranslation(a => a.TextPrompt, language.Id)}</a>";
                                }

                                formattedAttribute = $"{attributeName}: {attributeText}";
                            }

                            break;
                        }
                        default:
                        {
                            //other attributes (text box, datepicker)
                            formattedAttribute = $"{attribute.GetTranslation(a => a.Name, language.Id)}: {valueStr}";
                            //encode (if required)
                            if (htmlEncode)
                                formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);
                            break;
                        }
                    }
                }
                else
                {
                    var attributeValue = attribute.ContactAttributeValues.FirstOrDefault(x => x.Id == valueStr);
                    if (attributeValue != null)
                        formattedAttribute =
                            $"{attribute.GetTranslation(a => a.Name, language.Id)}: {attributeValue.GetTranslation(a => a.Name, language.Id)}";

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

    public async Task<IList<CustomAttribute>> GetParseContactAttributes(IList<CustomAttribute> model)
    {
        var customAttributes = new List<CustomAttribute>();
        var contactAttributes = await _contactAttributeService.GetAllContactAttributes();
        foreach (var attribute in contactAttributes)
            switch (attribute.AttributeControlType)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                case AttributeControlType.ColorSquares:
                case AttributeControlType.ImageSquares:
                {
                    var ctrlAttributes = model.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(ctrlAttributes))
                        customAttributes = AddContactAttribute(customAttributes,
                            attribute, ctrlAttributes).ToList();
                }
                    break;
                case AttributeControlType.Checkboxes:
                {
                    var cblAttributes = model.FirstOrDefault(x => x.Key == attribute.Id)?.Value;

                    if (!string.IsNullOrEmpty(cblAttributes))
                        foreach (var item in cblAttributes.Split(','))
                            customAttributes = AddContactAttribute(customAttributes, attribute, item).ToList();
                }
                    break;
                case AttributeControlType.ReadonlyCheckboxes:
                {
                    //load read-only (already server-side selected) values
                    var attributeValues = attribute.ContactAttributeValues;
                    foreach (var selectedAttributeId in attributeValues
                                 .Where(v => v.IsPreSelected)
                                 .Select(v => v.Id)
                                 .ToList())
                        customAttributes = AddContactAttribute(customAttributes,
                            attribute, selectedAttributeId).ToList();
                }
                    break;
                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                {
                    var ctrlAttributes = model.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(ctrlAttributes))
                    {
                        var enteredText = ctrlAttributes.Trim();
                        customAttributes = AddContactAttribute(customAttributes,
                            attribute, enteredText).ToList();
                    }
                }
                    break;
                case AttributeControlType.Datepicker:
                {
                    var date = model.FirstOrDefault(x => x.Key == attribute.Id + "_day")?.Value;
                    var month = model.FirstOrDefault(x => x.Key == attribute.Id + "_month")?.Value;
                    var year = model.FirstOrDefault(x => x.Key == attribute.Id + "_year")?.Value;
                    DateTime? selectedDate = null;
                    try
                    {
                        selectedDate = new DateTime(int.Parse(year), int.Parse(month), int.Parse(date));
                    }
                    catch { }

                    if (selectedDate.HasValue)
                        customAttributes = AddContactAttribute(customAttributes,
                            attribute, selectedDate.Value.ToString("D")).ToList();
                }
                    break;
                case AttributeControlType.FileUpload:
                {
                    var guid = model.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (Guid.TryParse(guid, out var downloadGuid))
                        customAttributes = AddContactAttribute(customAttributes,
                            attribute, downloadGuid.ToString()).ToList();
                }
                    break;
            }

        //validate conditional attributes (if specified)
        foreach (var attribute in contactAttributes)
        {
            var conditionMet = await IsConditionMet(attribute, customAttributes);
            if (conditionMet.HasValue && !conditionMet.Value)
                customAttributes = RemoveContactAttribute(customAttributes, attribute).ToList();
        }

        return customAttributes;
    }
}