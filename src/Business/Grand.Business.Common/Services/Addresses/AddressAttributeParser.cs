using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Localization;
using Grand.SharedKernel.Extensions;
using System.Net;

namespace Grand.Business.Common.Services.Addresses;

/// <summary>
///     Address attribute parser
/// </summary>
public class AddressAttributeParser : IAddressAttributeParser
{
    private readonly IAddressAttributeService _addressAttributeService;

    public AddressAttributeParser(
        IAddressAttributeService addressAttributeService)
    {
        _addressAttributeService = addressAttributeService;
    }

    /// <summary>
    ///     Gets selected address attributes
    /// </summary>
    /// <param name="customAttributes">Attributes</param>
    /// <returns>Selected address attributes</returns>
    public virtual async Task<IList<AddressAttribute>> ParseAddressAttributes(IList<CustomAttribute> customAttributes)
    {
        var result = new List<AddressAttribute>();
        if (!customAttributes.Any())
            return result;

        foreach (var customAttribute in customAttributes.GroupBy(x => x.Key))
        {
            var attribute = await _addressAttributeService.GetAddressAttributeById(customAttribute.Key);
            if (attribute != null) result.Add(attribute);
        }

        return result;
    }

    /// <summary>
    ///     Get address attribute values
    /// </summary>
    /// <param name="customAttributes">Attributes</param>
    /// <returns>Address attribute values</returns>
    public virtual async Task<IList<AddressAttributeValue>> ParseAddressAttributeValues(
        IList<CustomAttribute> customAttributes)
    {
        var values = new List<AddressAttributeValue>();
        if (!customAttributes.Any())
            return values;

        var attributes = await ParseAddressAttributes(customAttributes);
        foreach (var attribute in attributes)
        {
            if (!attribute.ShouldHaveValues())
                continue;

            var valuesStr = customAttributes.Where(x => x.Key == attribute.Id).Select(x => x.Value);
            values.AddRange(from valueStr in valuesStr
                where !string.IsNullOrEmpty(valueStr)
                select attribute.AddressAttributeValues.FirstOrDefault(x => x.Id == valueStr)
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
    /// <param name="attribute">Address attribute</param>
    /// <param name="value">Value</param>
    /// <returns>Attributes</returns>
    public virtual IList<CustomAttribute> AddAddressAttribute(IList<CustomAttribute> customAttributes,
        AddressAttribute attribute, string value)
    {
        customAttributes ??= new List<CustomAttribute>();
        customAttributes.Add(new CustomAttribute { Key = attribute.Id, Value = value });
        return customAttributes;
    }

    /// <summary>
    ///     Validates address attributes
    /// </summary>
    /// <param name="customAttributes">Attributes</param>
    /// <returns>Warnings</returns>
    public virtual async Task<IList<string>> GetAttributeWarnings(IList<CustomAttribute> customAttributes)
    {
        var warnings = new List<string>();

        //ensure it's our attributes
        var attributes1 = await ParseAddressAttributes(customAttributes);

        //validate required address attributes (whether they're chosen/selected/entered)
        var attributes2 = await _addressAttributeService.GetAllAddressAttributes();
        foreach (var a2 in attributes2)
        {
            if (!a2.IsRequired) continue;
            var found = false;
            //selected address attributes
            foreach (var a1 in attributes1)
            {
                if (a1.Id != a2.Id) continue;
                var valuesStr = customAttributes.Where(x => x.Key == a1.Id).Select(x => x.Value);
                if (valuesStr.Any(str1 => !string.IsNullOrEmpty(str1.Trim()))) found = true;
            }

            //if not found
            if (found) continue;
            warnings.Add("Selected attribute not found");
        }

        return warnings;
    }

    /// <summary>
    ///     Formats attributes
    /// </summary>
    /// <param name="language">Languages</param>
    /// <param name="customAttributes">Attributes</param>
    /// <param name="separator">Separator</param>
    /// <param name="htmlEncode">A value indicating whether to encode (HTML) values</param>
    /// <returns>Attributes</returns>
    public virtual async Task<string> FormatAttributes(
        Language language,
        IList<CustomAttribute> customAttributes,
        string separator = "<br />",
        bool htmlEncode = true)
    {
        var result = new StringBuilder();
        if (customAttributes == null)
            return result.ToString();

        var attributes = await ParseAddressAttributes(customAttributes);
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
                            break;
                        }
                        case AttributeControlType.FileUpload:
                            //file upload
                            //not supported for address attributes
                            break;
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
                    var attributeValueId = valueStr;
                    var attributeValue = attribute.AddressAttributeValues.FirstOrDefault(x => x.Id == attributeValueId);
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
}