using AutoMapper;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Common;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Common;

namespace Grand.Web.Admin.Services;

public class AddressAttributeViewModelService : IAddressAttributeViewModelService
{
    private readonly IAddressAttributeService _addressAttributeService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;
    private readonly IMapper _mapper;

    public AddressAttributeViewModelService(IAddressAttributeService addressAttributeService,
        ITranslationService translationService, IWorkContext workContext, IMapper mapper)
    {
        _addressAttributeService = addressAttributeService;
        _translationService = translationService;
        _workContext = workContext;
        _mapper = mapper;
    }

    public virtual async Task<(IEnumerable<AddressAttributeModel> addressAttributes, int totalCount)>
        PrepareAddressAttributes()
    {
        var addressAttributes = await _addressAttributeService.GetAllAddressAttributes();
        return (addressAttributes.Select(x =>
        {
            var attributeModel = _mapper.Map<AddressAttributeModel>(x);
            attributeModel.AttributeControlTypeName =
                x.AttributeControlType.GetTranslationEnum(_translationService, _workContext);
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
        var model = _mapper.Map<AddressAttributeModel>(addressAttribute);
        return model;
    }

    public virtual async Task<AddressAttribute> InsertAddressAttributeModel(AddressAttributeModel model)
    {
        var addressAttribute = _mapper.Map<AddressAttribute>(model);
        await _addressAttributeService.InsertAddressAttribute(addressAttribute);

        return addressAttribute;
    }

    public virtual async Task<AddressAttribute> UpdateAddressAttributeModel(AddressAttributeModel model,
        AddressAttribute addressAttribute)
    {
        addressAttribute = _mapper.Map(model, addressAttribute);
        await _addressAttributeService.UpdateAddressAttribute(addressAttribute);
        return addressAttribute;
    }

    public virtual async Task<(IEnumerable<AddressAttributeValueModel> addressAttributeValues, int totalCount)>
        PrepareAddressAttributeValues(string addressAttributeId)
    {
        var values = (await _addressAttributeService.GetAddressAttributeById(addressAttributeId))
            .AddressAttributeValues;
        return (values.Select(x => _mapper.Map<AddressAttributeValueModel>(x)), values.Count);
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
        var addressAttributeValue = _mapper.Map<AddressAttributeValue>(model);
        await _addressAttributeService.InsertAddressAttributeValue(addressAttributeValue);
        return addressAttributeValue;
    }

    public virtual AddressAttributeValueModel PrepareAddressAttributeValueModel(
        AddressAttributeValue addressAttributeValue)
    {
        var model = _mapper.Map<AddressAttributeValueModel>(addressAttributeValue);
        return model;
    }

    public virtual async Task<AddressAttributeValue> UpdateAddressAttributeValueModel(AddressAttributeValueModel model,
        AddressAttributeValue addressAttributeValue)
    {
        addressAttributeValue = _mapper.Map(model, addressAttributeValue);
        await _addressAttributeService.UpdateAddressAttributeValue(addressAttributeValue);
        return addressAttributeValue;
    }
}