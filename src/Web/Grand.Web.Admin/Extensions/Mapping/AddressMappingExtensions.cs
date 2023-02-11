﻿using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Infrastructure.Mapper;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Web.Admin.Models.Common;

namespace Grand.Web.Admin.Extensions
{
    public static class AddressMappingExtensions
    {
        public static async Task<AddressModel> ToModel(this Address entity, ICountryService countryService)
        {
            var address = entity.MapTo<Address, AddressModel>();
            var country = await countryService.GetCountryById(address.CountryId);
            if (country != null && !string.IsNullOrEmpty(address.CountryId))
            {
                address.CountryName = country?.Name;
            }
            if (country != null && !string.IsNullOrEmpty(address.StateProvinceId))
            {
                address.StateProvinceName = country?.StateProvinces.FirstOrDefault(x=>x.Id == address.StateProvinceId)?.Name;
            }

            return address;
        }

        public static Address ToEntity(this AddressModel model)
        {
            return model.MapTo<AddressModel, Address>();
        }

        public static Address ToEntity(this AddressModel model, Address destination)
        {
            return model.MapTo(destination);
        }

        public static async Task PrepareCustomAddressAttributes(this AddressModel model,
            Address address,
            IAddressAttributeService addressAttributeService,
            IAddressAttributeParser addressAttributeParser)
        {
            //this method is very similar to the same one in Grand.Web project
            if (addressAttributeService == null)
                throw new ArgumentNullException(nameof(addressAttributeService));

            if (addressAttributeParser == null)
                throw new ArgumentNullException(nameof(addressAttributeParser));

            var attributes = await addressAttributeService.GetAllAddressAttributes();
            foreach (var attribute in attributes)
            {
                var attributeModel = new AddressModel.AddressAttributeModel
                {
                    Id = attribute.Id,
                    Name = attribute.Name,
                    IsRequired = attribute.IsRequired,
                    AttributeControlType = attribute.AttributeControlType,
                };

                if (attribute.ShouldHaveValues())
                {
                    //values
                    var attributeValues = attribute.AddressAttributeValues;
                    foreach (var attributeValue in attributeValues)
                    {
                        var attributeValueModel = new AddressModel.AddressAttributeValueModel
                        {
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
                                var selectedValues = await addressAttributeParser.ParseAddressAttributeValues(selectedAddressAttributes);
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
                                var enteredText = selectedAddressAttributes.Where(x => x.Key == attribute.Id).Select(x => x.Value).ToList();
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
    }
}