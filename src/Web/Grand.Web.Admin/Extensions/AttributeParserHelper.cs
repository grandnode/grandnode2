using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Web.Admin.Models.Common;

namespace Grand.Web.Admin.Extensions;

/// <summary>
///     Parser helper
/// </summary>
public static class AttributeParserHelper
{
    public static async Task<List<CustomAttribute>> ParseCustomAddressAttributes(this AddressModel address,
        IAddressAttributeParser addressAttributeParser,
        IAddressAttributeService addressAttributeService)
    {
        ArgumentNullException.ThrowIfNull(address);

        var customAttributes = new List<CustomAttribute>();
        var attributes = await addressAttributeService.GetAllAddressAttributes();
        foreach (var attribute in attributes)
            switch (attribute.AttributeControlType)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                {
                    var ctrlAttributes = address.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(ctrlAttributes))
                        customAttributes = addressAttributeParser.AddAddressAttribute(customAttributes,
                            attribute, ctrlAttributes).ToList();
                }
                    break;
                case AttributeControlType.Checkboxes:
                {
                    var cblAttributes = address.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(cblAttributes))
                        foreach (var item in cblAttributes.Split(','))
                            if (!string.IsNullOrEmpty(item))
                                customAttributes = addressAttributeParser.AddAddressAttribute(customAttributes,
                                    attribute, item).ToList();
                }
                    break;
                case AttributeControlType.ReadonlyCheckboxes:
                {
                    //load read-only (already server-side selected) values
                    var attributeValues = attribute.AddressAttributeValues;
                    foreach (var selectedAttributeId in attributeValues
                                 .Where(v => v.IsPreSelected)
                                 .Select(v => v.Id)
                                 .ToList())
                        customAttributes = addressAttributeParser.AddAddressAttribute(customAttributes,
                            attribute, selectedAttributeId).ToList();
                }
                    break;
                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                {
                    var ctrlAttributes = address.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(ctrlAttributes))
                    {
                        var enteredText = ctrlAttributes.Trim();
                        customAttributes = addressAttributeParser.AddAddressAttribute(customAttributes,
                            attribute, enteredText).ToList();
                    }
                }
                    break;
                case AttributeControlType.Datepicker:
                case AttributeControlType.ColorSquares:
                case AttributeControlType.ImageSquares:
                case AttributeControlType.FileUpload:
                //not supported address attributes
                default:
                    break;
            }

        return customAttributes;
    }
}