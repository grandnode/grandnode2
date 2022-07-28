﻿using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Web.Features.Models.Customers;
using MediatR;

namespace Grand.Web.Features.Handlers.Customers
{
    public class GetParseCustomAttributesHandler : IRequestHandler<GetParseCustomAttributes, IList<CustomAttribute>>
    {
        private readonly ICustomerAttributeService _customerAttributeService;
        private readonly ICustomerAttributeParser _customerAttributeParser;

        public GetParseCustomAttributesHandler(ICustomerAttributeService customerAttributeService, ICustomerAttributeParser customerAttributeParser)
        {
            _customerAttributeService = customerAttributeService;
            _customerAttributeParser = customerAttributeParser;
        }

        public async Task<IList<CustomAttribute>> Handle(GetParseCustomAttributes request, CancellationToken cancellationToken)
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
                string controlId = string.Format("customer_attribute_{0}", attribute.Id);
                switch (attribute.AttributeControlTypeId)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                        {
                            request.Form.TryGetValue(controlId, out var ctrlAttributes);
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                            {
                                customAttributes = _customerAttributeParser.AddCustomerAttribute(customAttributes,
                                    attribute, ctrlAttributes).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.Checkboxes:
                        {
                            request.Form.TryGetValue(controlId, out var cblAttributes);
                            if (!string.IsNullOrEmpty(cblAttributes))
                            {
                                foreach (var item in cblAttributes)
                                {
                                    if (!String.IsNullOrEmpty(item))
                                        customAttributes = _customerAttributeParser.AddCustomerAttribute(customAttributes,
                                            attribute, item).ToList();
                                }
                            }
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
                            {
                                customAttributes = _customerAttributeParser.AddCustomerAttribute(customAttributes,
                                            attribute, selectedAttributeId).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                    case AttributeControlType.Hidden:
                        {
                            request.Form.TryGetValue(controlId, out var ctrlAttributes);
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                            {
                                var enteredText = ctrlAttributes.ToString().Trim();
                                customAttributes = _customerAttributeParser.AddCustomerAttribute(customAttributes,
                                    attribute, enteredText).ToList();
                            }
                            else
                                customAttributes = _customerAttributeParser.AddCustomerAttribute(customAttributes,
                                    attribute, "").ToList();

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
}
