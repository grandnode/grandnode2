using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Web.Features.Models.Common;
using MediatR;

namespace Grand.Web.Features.Handlers.Common;

public class
    GetParseCustomAddressAttributesHandler : IRequestHandler<GetParseCustomAddressAttributes, IList<CustomAttribute>>
{
    private readonly IAddressAttributeParser _addressAttributeParser;
    private readonly IAddressAttributeService _addressAttributeService;

    public GetParseCustomAddressAttributesHandler(
        IAddressAttributeService addressAttributeService,
        IAddressAttributeParser addressAttributeParser)
    {
        _addressAttributeService = addressAttributeService;
        _addressAttributeParser = addressAttributeParser;
    }

    public async Task<IList<CustomAttribute>> Handle(GetParseCustomAddressAttributes request,
        CancellationToken cancellationToken)
    {
        if (request.SelectedAttributes == null)
            throw new ArgumentNullException(nameof(request.SelectedAttributes));

        var customAttributes = new List<CustomAttribute>();
        var attributes = await _addressAttributeService.GetAllAddressAttributes();
        foreach (var attribute in attributes)
            switch (attribute.AttributeControlType)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                {
                    var ctrlAttributes = request.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(ctrlAttributes))
                        customAttributes = _addressAttributeParser.AddAddressAttribute(customAttributes,
                            attribute, ctrlAttributes).ToList();
                }
                    break;
                case AttributeControlType.Checkboxes:
                {
                    var cblAttributes = request.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(cblAttributes))
                        foreach (var item in cblAttributes.Split(','))
                            if (!string.IsNullOrEmpty(item))
                                customAttributes = _addressAttributeParser.AddAddressAttribute(customAttributes,
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
                        customAttributes = _addressAttributeParser.AddAddressAttribute(customAttributes,
                            attribute, selectedAttributeId).ToList();
                }
                    break;
                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                {
                    var ctrlAttributes = request.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(ctrlAttributes))
                    {
                        var enteredText = ctrlAttributes.Trim();
                        customAttributes = _addressAttributeParser.AddAddressAttribute(customAttributes,
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