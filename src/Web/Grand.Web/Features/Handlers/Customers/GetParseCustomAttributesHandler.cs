using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Web.Features.Models.Customers;
using MediatR;

namespace Grand.Web.Features.Handlers.Customers;

public class GetParseCustomAttributesHandler : IRequestHandler<GetParseCustomAttributes, IList<CustomAttribute>>
{
    private readonly ICustomerAttributeParser _customerAttributeParser;
    private readonly ICustomerAttributeService _customerAttributeService;

    public GetParseCustomAttributesHandler(ICustomerAttributeService customerAttributeService,
        ICustomerAttributeParser customerAttributeParser)
    {
        _customerAttributeService = customerAttributeService;
        _customerAttributeParser = customerAttributeParser;
    }

    public async Task<IList<CustomAttribute>> Handle(GetParseCustomAttributes request,
        CancellationToken cancellationToken)
    {
        var customAttributes = new List<CustomAttribute>();
        var attributes = await _customerAttributeService.GetAllCustomerAttributes();
        foreach (var attribute in attributes)
        {
            if (attribute.IsReadOnly)
            {
                var attrReadOnly = request.CustomerCustomAttribute.FirstOrDefault(x => x.Key == attribute.Id);
                if (attrReadOnly != null)
                    customAttributes.Add(attrReadOnly);

                continue;
            }

            switch (attribute.AttributeControlTypeId)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                {
                    var ctrlAttributes = request.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(ctrlAttributes))
                        customAttributes = _customerAttributeParser.AddCustomerAttribute(customAttributes,
                            attribute, ctrlAttributes).ToList();
                }
                    break;
                case AttributeControlType.Checkboxes:
                {
                    var cblAttributes = request.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(cblAttributes))
                        foreach (var item in cblAttributes.Split(','))
                            if (!string.IsNullOrEmpty(item))
                                customAttributes = _customerAttributeParser.AddCustomerAttribute(customAttributes,
                                    attribute, item).ToList();
                }
                    break;
                case AttributeControlType.ReadonlyCheckboxes:
                {
                    //load read-only (already server-side selected) values
                    var attributeValues = attribute.CustomerAttributeValues;
                    foreach (var selectedAttributeId in attributeValues
                                 .Where(v => v.IsPreSelected)
                                 .Select(v => v.Id)
                                 .ToList())
                        customAttributes = _customerAttributeParser.AddCustomerAttribute(customAttributes,
                            attribute, selectedAttributeId).ToList();
                }
                    break;
                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                case AttributeControlType.Hidden:
                {
                    var ctrlAttributes = request.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(ctrlAttributes))
                    {
                        var enteredText = ctrlAttributes.Trim();
                        customAttributes = _customerAttributeParser.AddCustomerAttribute(customAttributes,
                            attribute, enteredText).ToList();
                    }
                    else
                    {
                        customAttributes = _customerAttributeParser.AddCustomerAttribute(customAttributes,
                            attribute, "").ToList();
                    }
                }
                    break;
                case AttributeControlType.Datepicker:
                case AttributeControlType.ColorSquares:
                case AttributeControlType.ImageSquares:
                case AttributeControlType.FileUpload:
                //not supported customer attributes
                default:
                    break;
            }
        }

        return customAttributes;
    }
}