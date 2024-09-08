using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Domain.Common;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Common.Localization;

namespace Grand.Web.Admin.Services;

public class AddressAttributeViewModelService(
    IAddressAttributeService addressAttributeService,
    IEnumTranslationService enumTranslationService)
    : IAddressAttributeViewModelService
{
    public virtual async Task<(IEnumerable<AddressAttributeModel> addressAttributes, int totalCount)>
        PrepareAddressAttributes()
    {
        var addressAttributes = await addressAttributeService.GetAllAddressAttributes();
        return (addressAttributes.Select(x =>
        {
            var attributeModel = x.ToModel();
            attributeModel.AttributeControlTypeName = enumTranslationService.GetTranslationEnum(x.AttributeControlType);
            return attributeModel;
        }), addressAttributes.Count);
    }

    public virtual AddressAttributeModel PrepareAddressAttributeModel()
    {
        var model = new AddressAttributeModel();
        return model;
    }

    public virtual AddressAttributeModel PrepareAddressAttributeModel(AddressAttribute addressAttribute)
    {
        var model = addressAttribute.ToModel();
        return model;
    }

    public virtual async Task<AddressAttribute> InsertAddressAttributeModel(AddressAttributeModel model)
    {
        var addressAttribute = model.ToEntity();
        await addressAttributeService.InsertAddressAttribute(addressAttribute);

        return addressAttribute;
    }

    public virtual async Task<AddressAttribute> UpdateAddressAttributeModel(AddressAttributeModel model,
        AddressAttribute addressAttribute)
    {
        addressAttribute = model.ToEntity(addressAttribute);
        await addressAttributeService.UpdateAddressAttribute(addressAttribute);
        return addressAttribute;
    }

    public virtual async Task<(IEnumerable<AddressAttributeValueModel> addressAttributeValues, int totalCount)>
        PrepareAddressAttributeValues(string addressAttributeId)
    {
        var values = (await addressAttributeService.GetAddressAttributeById(addressAttributeId))
            .AddressAttributeValues;
        return (values.Select(x => x.ToModel()), values.Count);
    }

    public virtual AddressAttributeValueModel PrepareAddressAttributeValueModel(string addressAttributeId)
    {
        var model = new AddressAttributeValueModel {
            AddressAttributeId = addressAttributeId
        };
        return model;
    }

    public virtual async Task<AddressAttributeValue> InsertAddressAttributeValueModel(AddressAttributeValueModel model)
    {
        var addressAttributeValue = model.ToEntity();
        await addressAttributeService.InsertAddressAttributeValue(addressAttributeValue);
        return addressAttributeValue;
    }

    public virtual AddressAttributeValueModel PrepareAddressAttributeValueModel(
        AddressAttributeValue addressAttributeValue)
    {
        var model = addressAttributeValue.ToModel();
        return model;
    }

    public virtual async Task<AddressAttributeValue> UpdateAddressAttributeValueModel(AddressAttributeValueModel model,
        AddressAttributeValue addressAttributeValue)
    {
        addressAttributeValue = model.ToEntity(addressAttributeValue);
        await addressAttributeService.UpdateAddressAttributeValue(addressAttributeValue);
        return addressAttributeValue;
    }
}