using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Infrastructure.Mapper;
using Grand.Web.Vendor.Models.Common;

namespace Grand.Web.Vendor.Extensions;

public static class AddressMappingExtensions
{
    public static async Task<AddressModel> ToModel(this Address entity, ICountryService countryService)
    {
        var address = entity.MapTo<Address, AddressModel>();
        var country = await countryService.GetCountryById(address.CountryId);
        if (country != null && !string.IsNullOrEmpty(address.CountryId)) address.CountryName = country?.Name;

        if (country != null && !string.IsNullOrEmpty(address.StateProvinceId))
            address.StateProvinceName =
                country?.StateProvinces.FirstOrDefault(x => x.Id == address.StateProvinceId)?.Name;

        return address;
    }

    public static Address ToEntity(this AddressModel model)
    {
        return model.MapTo<AddressModel, Address>();
    }

    public static async Task PrepareCustomAddressAttributes(this AddressModel model,
        Address address,
        IAddressAttributeService addressAttributeService,
        IAddressAttributeParser addressAttributeParser)
    {
        ArgumentNullException.ThrowIfNull(addressAttributeService);
        ArgumentNullException.ThrowIfNull(addressAttributeParser);

        var attributes = await addressAttributeService.GetAllAddressAttributes();
        foreach (var attribute in attributes)
        {
            var attributeModel = new AddressModel.AddressAttributeModel {
                Id = attribute.Id,
                Name = attribute.Name,
                IsRequired = attribute.IsRequired,
                AttributeControlType = attribute.AttributeControlType
            };

            if (attribute.ShouldHaveValues())
            {
                //values
                var attributeValues = attribute.AddressAttributeValues;
                foreach (var attributeValue in attributeValues)
                {
                    var attributeValueModel = new AddressModel.AddressAttributeValueModel {
                        Id = attributeValue.Id,
                        Name = attributeValue.Name,
                        IsPreSelected = attributeValue.IsPreSelected
                    };
                    attributeModel.Values.Add(attributeValueModel);
                }
            }

            //set already selected attributes
            var selectedAddressAttributes = address != null ? address.Attributes : new List<CustomAttribute>();
            switch (attribute.AttributeControlType)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                case AttributeControlType.Checkboxes:
                {
                    if (selectedAddressAttributes.Any())
                    {
                        //clear default selection
                        foreach (var item in attributeModel.Values)
                            item.IsPreSelected = false;

                        //select new values
                        var selectedValues =
                            await addressAttributeParser.ParseAddressAttributeValues(selectedAddressAttributes);
                        foreach (var attributeValue in selectedValues)
                            if (attributeModel.Id == attributeValue.AddressAttributeId)
                                foreach (var item in attributeModel.Values)
                                    if (attributeValue.Id == item.Id)
                                        item.IsPreSelected = true;
                    }
                }
                    break;
                case AttributeControlType.ReadonlyCheckboxes:
                {
                    //do nothing
                    //values are already pre-set
                }
                    break;
                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                {
                    if (selectedAddressAttributes.Any())
                    {
                        var enteredText = selectedAddressAttributes.Where(x => x.Key == attribute.Id)
                            .Select(x => x.Value).ToList();
                        if (enteredText.Count > 0)
                            attributeModel.DefaultValue = enteredText[0];
                    }
                }
                    break;
                case AttributeControlType.ColorSquares:
                case AttributeControlType.Datepicker:
                case AttributeControlType.FileUpload:
                case AttributeControlType.ImageSquares:
                default:
                    //not supported attribute control types
                    break;
            }

            model.CustomAddressAttributes.Add(attributeModel);
        }
    }

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
                    var ctrlAttributes = address.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)
                        ?.Value;
                    if (!string.IsNullOrEmpty(ctrlAttributes))
                        customAttributes = addressAttributeParser.AddAddressAttribute(customAttributes,
                            attribute, ctrlAttributes).ToList();
                }
                    break;
                case AttributeControlType.Checkboxes:
                {
                    var cblAttributes = address.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)
                        ?.Value;
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
                    var ctrlAttributes = address.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)
                        ?.Value;
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