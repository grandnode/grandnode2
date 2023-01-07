﻿using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using MediatR;

namespace Grand.Web.Features.Handlers.Customers
{
    public class GetCustomAttributesHandler : IRequestHandler<GetCustomAttributes, IList<CustomerAttributeModel>>
    {
        private readonly ICustomerAttributeService _customerAttributeService;
        private readonly ICustomerAttributeParser _customerAttributeParser;

        public GetCustomAttributesHandler(ICustomerAttributeService customerAttributeService, ICustomerAttributeParser customerAttributeParser)
        {
            _customerAttributeService = customerAttributeService;
            _customerAttributeParser = customerAttributeParser;
        }

        public async Task<IList<CustomerAttributeModel>> Handle(GetCustomAttributes request, CancellationToken cancellationToken)
        {
            var result = new List<CustomerAttributeModel>();

            var customerAttributes = await _customerAttributeService.GetAllCustomerAttributes();
            foreach (var attribute in customerAttributes)
            {
                var attributeModel = new CustomerAttributeModel {
                    Id = attribute.Id,
                    Name = attribute.GetTranslation(x => x.Name, request.Language.Id),
                    IsRequired = attribute.IsRequired,
                    IsReadOnly = attribute.IsReadOnly,
                    AttributeControlType = attribute.AttributeControlTypeId
                };

                if (attribute.ShouldHaveValues())
                {
                    //values
                    var attributeValues = attribute.CustomerAttributeValues;
                    foreach (var attributeValue in attributeValues)
                    {
                        var valueModel = new CustomerAttributeValueModel {
                            Id = attributeValue.Id,
                            Name = attributeValue.GetTranslation(x => x.Name, request.Language.Id),
                            IsPreSelected = attributeValue.IsPreSelected
                        };
                        attributeModel.Values.Add(valueModel);
                    }
                }

                //set already selected attributes
                var selectedAttributes = request.OverrideAttributes ?? request.Customer.Attributes;

                switch (attribute.AttributeControlTypeId)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.Checkboxes:
                        {
                            if (selectedAttributes.Any())
                            {
                                //clear default selection
                                foreach (var item in attributeModel.Values)
                                    item.IsPreSelected = false;

                                //select new values
                                var selectedValues = await _customerAttributeParser.ParseCustomerAttributeValues(selectedAttributes);
                                foreach (var attributeValue in selectedValues)
                                    if (attributeModel.Id == attributeValue.CustomerAttributeId)
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
                            if (selectedAttributes.Any())
                            {
                                var enteredText = selectedAttributes.Where(x => x.Key == attribute.Id).Select(x => x.Value).ToList();
                                if (enteredText.Any())
                                    attributeModel.DefaultValue = enteredText[0];
                            }
                        }
                        break;
                    case AttributeControlType.ColorSquares:
                    case AttributeControlType.ImageSquares:
                    case AttributeControlType.Datepicker:
                    case AttributeControlType.FileUpload:
                    default:
                        //not supported attribute control types
                        break;
                }

                result.Add(attributeModel);
            }


            return result;
        }
    }
}
